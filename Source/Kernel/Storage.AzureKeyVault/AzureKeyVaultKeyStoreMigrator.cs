// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStoreMigrator"/> for Azure Key Vault.
/// </summary>
/// <remarks>
/// <para>
/// Every secret an earlier version stored under <c language="csharp">chronicle--{eventStore}--{namespace}--{identifier}--{suffix}</c>
/// that can be tied to one address through the scopes an operator listed is copied, as it is, to its hashed name. The erasure record
/// of an address is copied before its keys, and a key the erasure covers is never copied. Nothing is written over an existing secret,
/// over a name held by a deleted secret that can be recovered, or for an entry that carries an expiry, a not-before time, a content
/// type, tags, a disabled version or several versions: those are left for review. Names that cannot be tied to one address are never touched.
/// </para>
/// <para>
/// Every copy is read back and compared. A secret under an earlier name is removed only when asked, only after its copy verified,
/// only when nothing in the run failed or was left for review, and only when it is still what was copied. The migration expects every
/// node to be stopped; Key Vault has no conditional write, so it is not atomic with other writers, and a copy that gained a second
/// version during the run is left for review.
/// </para>
/// </remarks>
/// <param name="vault">The <see cref="ISecretVault"/> to talk to the vault through.</param>
/// <param name="scopes">The event store and namespace pairs an operator listed as holding keys under earlier names.</param>
/// <param name="catalog">The optional <see cref="IEncryptionKeyScopeCatalog"/> the list is cross-checked against.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
internal class AzureKeyVaultKeyStoreMigrator(
    ISecretVault vault,
    IReadOnlyList<LegacyScope> scopes,
    IEncryptionKeyScopeCatalog? catalog,
    ILogger<AzureKeyVaultKeyStoreMigrator>? logger = null) : IEncryptionKeyStoreMigrator
{
    readonly ILogger<AzureKeyVaultKeyStoreMigrator> _logger = logger ?? NullLogger<AzureKeyVaultKeyStoreMigrator>.Instance;
    readonly MigrationOwnership _ownership = new(vault);

    /// <inheritdoc/>
    public async Task<EncryptionKeyMigrationReport> Migrate(bool dryRun, bool removeVerifiedLegacy, CancellationToken cancellationToken = default)
    {
        var run = new MigrationRun(dryRun);
        if (scopes.Count == 0)
        {
            _logger.NoScopesListed();
            run.Failed++;
            return run.Report(complete: false);
        }

        var owner = Guid.NewGuid().ToString("N");
        var ownsLock = false;
        try
        {
            ownsLock = !dryRun && await _ownership.TryAcquire(owner, cancellationToken);
            if (!dryRun && !ownsLock)
            {
                return run.Report(complete: false) with { MigrationLocked = true };
            }

            await Execute(run, removeVerifiedLegacy, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Counts and the kind of failure only: what the vault answers may hold names.
            run.Failed++;
            _logger.MigrationStopped(exception.GetType().Name);
        }
        finally
        {
            if (ownsLock)
            {
                await Release(owner, run);
            }
        }

        return run.Report(complete: run.Failed == 0);
    }

    static bool SameValue(SecretValue? left, SecretValue? right) =>
        left is not null && right is not null && string.Equals(left.Value, right.Value, StringComparison.Ordinal);

    static CopyOutcome? Screen(IReadOnlyList<SecretVersionInfo> versions) => versions switch
    {
        { Count: 0 } => CopyOutcome.ConflictKept,
        { Count: 1 } when !versions[0].HasAttributes => null,
        _ => CopyOutcome.Held
    };

    static bool IsWithheld(LegacyEntry entry, SecretValue? pinned, CopyContext context) =>
        pinned is null ||
        context.Deleted.Contains(entry.Destination) ||
        (!entry.Name.IsErasure && LegacyErasureFence.AnyCovers(context.Fences, entry.Name.Revision, pinned.Value));

    async Task Release(string owner, MigrationRun run)
    {
        try
        {
            await _ownership.Release(owner, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            run.Failed++;
            _logger.MigrationStopped(exception.GetType().Name);
        }
    }

    async Task Execute(MigrationRun run, bool removeVerifiedLegacy, CancellationToken cancellationToken)
    {
        var entries = await Collect(run, cancellationToken);
        CheckEveryScopeHasASource(run, entries);
        await CrossCheckCatalog(run);

        var deleted = await vault.ListDeleted(cancellationToken);
        foreach (var address in entries.GroupBy(_ => (_.Name.EventStore, _.Name.EventStoreNamespace, _.Name.Identifier)))
        {
            var ordered = address.OrderByDescending(_ => _.Name.IsErasure).ThenBy(_ => _.Name.Revision).ToList();
            await MigrateAddress(run, ordered, deleted, cancellationToken);
        }

        if (removeVerifiedLegacy && !run.DryRun && run.Clean)
        {
            await RemoveVerified(run, cancellationToken);
        }
    }

    async Task<List<LegacyEntry>> Collect(MigrationRun run, CancellationToken cancellationToken)
    {
        var entries = new List<LegacyEntry>();
        foreach (var secret in (await vault.ListLive(cancellationToken)).Where(_ => LegacySecretName.IsEarlier(_.Name)))
        {
            run.Scanned++;
            if (LegacySecretName.Resolve(secret.Name, scopes) is { } resolved)
            {
                entries.Add(new(resolved, secret.Name));
            }
            else
            {
                run.AmbiguousForReview++;
            }
        }

        return entries;
    }

    void CheckEveryScopeHasASource(MigrationRun run, List<LegacyEntry> entries)
    {
        var withoutSource = scopes.LongCount(scope => !entries.Exists(_ =>
            _.Name.EventStore.Value == scope.EventStore && _.Name.EventStoreNamespace.Value == scope.EventStoreNamespace));
        if (withoutSource > 0)
        {
            run.Failed += withoutSource;
            _logger.ScopesWithoutSource(withoutSource);
        }
    }

    async Task CrossCheckCatalog(MigrationRun run)
    {
        if (catalog is null)
        {
            return;
        }

        try
        {
            var known = await catalog.GetScopes();
            run.ScopesMissingFromList = known.Count(scope => !scopes.Contains(scope));
            if (run.ScopesMissingFromList > 0)
            {
                _logger.ScopesNotListed(run.ScopesMissingFromList);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The catalog is a cross-check only; nothing depends on it.
        }
    }

    async Task MigrateAddress(MigrationRun run, List<LegacyEntry> entries, IReadOnlySet<string> deleted, CancellationToken cancellationToken)
    {
        var context = new CopyContext(run.DryRun, deleted, await ReadFences(entries, cancellationToken));
        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (outcome, verified) = await Copy(entry, context, cancellationToken);
            run.Count(entry.Name.IsErasure, outcome);
            if (verified is not null)
            {
                run.Verified.Add(verified);
            }
        }
    }

    async Task<IReadOnlyList<LegacyErasureFence>> ReadFences(List<LegacyEntry> entries, CancellationToken cancellationToken)
    {
        var fences = new List<LegacyErasureFence>();
        var names = new[] { entries.Find(_ => _.Name.IsErasure)?.Source, entries[0].Name.DestinationErasure };
        foreach (var name in names.OfType<string>())
        {
            if (await vault.Read(name, null, cancellationToken) is { } secret)
            {
                fences.Add(LegacyErasureFence.Parse(secret.Value));
            }
        }

        return fences;
    }

    async Task<(CopyOutcome Outcome, VerifiedCopy? Verified)> Copy(LegacyEntry entry, CopyContext context, CancellationToken cancellationToken)
    {
        var versions = await vault.ListVersions(entry.Source, cancellationToken);
        if (Screen(versions) is { } early)
        {
            return (early, null);
        }

        var pinned = await vault.Read(entry.Source, versions[0].Version, cancellationToken);
        if (IsWithheld(entry, pinned, context))
        {
            return (CopyOutcome.ConflictKept, null);
        }

        return await Place(entry, pinned!, context.DryRun, cancellationToken);
    }

    async Task<(CopyOutcome Outcome, VerifiedCopy? Verified)> Place(LegacyEntry entry, SecretValue pinned, bool dryRun, CancellationToken cancellationToken)
    {
        var verified = new VerifiedCopy(entry, pinned.Info.Version);
        if (await vault.Read(entry.Destination, null, cancellationToken) is not null)
        {
            return await DestinationCheck.EqualsSource(vault, entry.Destination, pinned, cancellationToken)
                ? (CopyOutcome.AlreadyMigrated, verified)
                : (CopyOutcome.ConflictKept, null);
        }

        if (dryRun)
        {
            return (CopyOutcome.Copied, null);
        }

        return await Write(entry, pinned, cancellationToken) ? (CopyOutcome.Copied, verified) : (CopyOutcome.ConflictKept, null);
    }

    async Task<bool> Write(LegacyEntry entry, SecretValue pinned, CancellationToken cancellationToken)
    {
        var written = await vault.Write(entry.Destination, pinned.Value, cancellationToken);
        if (written is null)
        {
            return false;
        }

        // Another writer that appended a version of the same name leaves it for review: no election decides here.
        var copies = await vault.ListVersions(entry.Destination, cancellationToken);
        return copies.Count == 1 && SameValue(pinned, await vault.Read(entry.Destination, written, cancellationToken));
    }

    async Task RemoveVerified(MigrationRun run, CancellationToken cancellationToken)
    {
        foreach (var verified in run.Verified.OrderBy(_ => _.Entry.Name.IsErasure))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RemoveLegacy(run, verified, cancellationToken);
        }
    }

    async Task RemoveLegacy(MigrationRun run, VerifiedCopy copy, CancellationToken cancellationToken)
    {
        if (!await IsUnchanged(copy, cancellationToken))
        {
            run.ConflictsKept++;
            return;
        }

        if (await vault.Remove(copy.Entry.Source, cancellationToken) == SecretRemoval.Purged)
        {
            run.LegacyRemoved++;
        }
        else
        {
            run.LegacyRemovedNotPurged++;
        }
    }

    /// <summary>
    /// The earlier secret goes only when it is still exactly what was copied, and the copy is still the same.
    /// </summary>
    /// <param name="copy">The <see cref="VerifiedCopy"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>True when nothing changed.</returns>
    async Task<bool> IsUnchanged(VerifiedCopy copy, CancellationToken cancellationToken)
    {
        var versions = await vault.ListVersions(copy.Entry.Source, cancellationToken);
        if (versions.Count != 1 || versions[0].Version != copy.SourceVersion)
        {
            return false;
        }

        var latest = await vault.Read(copy.Entry.Source, null, cancellationToken);
        return latest is not null && await DestinationCheck.EqualsSource(vault, copy.Entry.Destination, latest, cancellationToken);
    }

    sealed record CopyContext(bool DryRun, IReadOnlySet<string> Deleted, IReadOnlyList<LegacyErasureFence> Fences);
}
