// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VaultSharp;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStoreMigrator"/> for HashiCorp Vault.
/// </summary>
/// <remarks>
/// <para>
/// Every secret an earlier version stored for an address that can be tied to it alone is copied, as it is, to its path
/// under <see cref="VaultPathSegments.Root"/>, with the layout recorded. The erasure record of an address is copied
/// before its keys, and a key the erasure covers is never copied. A copy is written only where nothing exists yet, so
/// nothing under <see cref="VaultPathSegments.Root"/> is ever overwritten; what is there already and differs is left for
/// review. Secrets that cannot be tied to one address are never touched.
/// </para>
/// <para>
/// Every copy is verified after the run. Secrets under earlier names are removed only when asked, only after their copy
/// verified, and only when nothing in the run failed or was left for review. The migration expects every node to be
/// stopped; it does not make the copy atomic with other writers.
/// </para>
/// </remarks>
/// <param name="store">The <see cref="IVaultSecretStore"/> to talk to Vault through.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
internal class VaultKeyStoreMigrator(IVaultSecretStore store, ILogger<VaultKeyStoreMigrator> logger) : IEncryptionKeyStoreMigrator
{
    const string PublicKeyField = "publicKey";
    const string ErasedThroughField = "erasedThrough";
    const string ErasedKeyFingerprintsField = "erasedKeyFingerprints";

    readonly VaultKeyStoreUpgradeCheck _check = new(store);

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultKeyStoreMigrator"/> class over a client.
    /// </summary>
    /// <param name="vaultClient">The <see cref="IVaultClient"/> to talk to Vault through.</param>
    /// <param name="mountPointName">KV v2 mount point. Defaults to <c language="csharp">secret</c> when <see langword="null"/> or empty.</param>
    /// <param name="logger">Optional <see cref="ILogger"/>.</param>
    public VaultKeyStoreMigrator(IVaultClient vaultClient, string? mountPointName = null, ILogger<VaultKeyStoreMigrator>? logger = null)
        : this(new VaultSecretStore(vaultClient, mountPointName), logger ?? NullLogger<VaultKeyStoreMigrator>.Instance)
    {
    }

    enum Outcome
    {
        AlreadyMigrated = 0,
        Copied = 1,
        ConflictKept = 2,
        Ambiguous = 3
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyMigrationReport> Migrate(bool dryRun, bool removeVerifiedLegacy, CancellationToken cancellationToken = default)
    {
        var layout = dryRun ? await _check.InspectLayout() : await _check.EnsureLayout();
        var run = new Run(dryRun);
        if (layout.LayoutUnusable)
        {
            return run.Report(layoutUnusable: true, complete: false);
        }

        var complete = true;
        try
        {
            await CopyAll(run, cancellationToken);
            if (!dryRun)
            {
                await Verify(run, cancellationToken);
            }

            if (!dryRun && removeVerifiedLegacy && run.Failed == 0 && run.ConflictsKept == 0)
            {
                await RemoveVerified(run, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Counts and the kind of failure only: what the key store answers may hold paths.
            run.Failed++;
            complete = false;
            logger.MigrationStopped(exception.GetType().Name);
        }

        return run.Report(layoutUnusable: false, complete: complete);
    }

    static (bool Present, Fence? Fence) ReadFence(VaultSecret? erasure)
    {
        if (erasure is null)
        {
            return (false, null);
        }

        if (!erasure.Data.TryGetValue(ErasedThroughField, out var erasedThrough) ||
            !uint.TryParse(erasedThrough, NumberStyles.None, CultureInfo.InvariantCulture, out var floor))
        {
            return (true, null);
        }

        var fingerprints = erasure.Data.TryGetValue(ErasedKeyFingerprintsField, out var raw)
            ? raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            : [];
        return (true, new Fence(floor, fingerprints));
    }

    static bool IsFenced(uint revision, VaultSecret key, IEnumerable<(bool Present, Fence? Fence)> fences)
    {
        string? fingerprint = null;
        foreach (var (present, fence) in fences.Where(_ => _.Present))
        {
            // An erasure record that cannot be read could cover anything, so nothing is copied past it.
            if (fence is null || revision <= fence.ErasedThrough)
            {
                return true;
            }

            fingerprint ??= Fingerprint(key);
            if (fingerprint is null || fence.ErasedKeyFingerprints.Contains(fingerprint, StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    static Outcome Compare(VaultSecret legacy, VaultSecret current) =>
        VaultLegacyLayout.SameContent(legacy, current) ? Outcome.AlreadyMigrated : Outcome.ConflictKept;

    static string? Fingerprint(VaultSecret key)
    {
        if (!key.Data.TryGetValue(PublicKeyField, out var publicKey))
        {
            return null;
        }

        try
        {
            return new EncryptionKey(Convert.FromBase64String(publicKey), []).Fingerprint;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    async Task CopyAll(Run run, CancellationToken cancellationToken)
    {
        var earlierRoots = (await store.List(string.Empty)).Where(VaultLegacyLayout.IsEarlierEntry).ToList();
        var entries = await VaultLegacyLayout.Walk(store, earlierRoots, cancellationToken);
        run.Scanned = entries.Count;
        run.AmbiguousForReview = entries.Count(_ => _.IsAmbiguous);

        foreach (var address in entries.Where(_ => !_.IsAmbiguous).GroupBy(_ => _.Directory, StringComparer.Ordinal))
        {
            await CopyAddress([.. address], run, cancellationToken);
        }
    }

    async Task CopyAddress(List<VaultLegacyEntry> entries, Run run, CancellationToken cancellationToken)
    {
        // The erasure record goes first, so a key it covers is never readable under the new names without it.
        var erasure = entries.FirstOrDefault(_ => _.IsErasure);
        if (erasure is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            run.Count(erasure, await Copy(erasure, run.DryRun));
        }

        // An erasure under either name covers the key: one recorded by this version may exist without an earlier one.
        var fences = new List<(bool Present, Fence? Fence)>
        {
            ReadFence(await store.Read($"{entries[0].Directory}/{VaultKeyLocations.ErasureLeaf}"))
        };
        if (erasure is not null)
        {
            fences.Add(ReadFence(await store.Read(erasure.Path)));
        }

        foreach (var key in entries.Where(_ => !_.IsErasure).OrderBy(_ => _.Revision))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await store.Read(key.Path) is { } legacy && IsFenced(key.Revision!.Value, legacy, fences))
            {
                run.Count(key, Outcome.ConflictKept);
                continue;
            }

            run.Count(key, await Copy(key, run.DryRun));
        }
    }

    async Task<Outcome> Copy(VaultLegacyEntry entry, bool dryRun)
    {
        if (await store.Read(entry.Path) is not { } legacy)
        {
            // It was listed a moment ago and is gone: something else is writing to the key store.
            return Outcome.ConflictKept;
        }

        if (!legacy.OnlyText)
        {
            return Outcome.Ambiguous;
        }

        if (await store.Read(entry.Destination!) is { } existing)
        {
            return Compare(legacy, existing);
        }

        if (dryRun)
        {
            return Outcome.Copied;
        }

        if (await store.WriteIfAbsent(entry.Destination!, VaultLegacyLayout.CopyOf(legacy)) is not { } version)
        {
            // Another writer got there first; whatever it wrote is never overwritten.
            return await store.Read(entry.Destination!) is { } winner ? Compare(legacy, winner) : Outcome.ConflictKept;
        }

        if (await store.Read(entry.Path) is { } again && VaultLegacyLayout.SameContent(legacy, again))
        {
            return Outcome.Copied;
        }

        // The earlier secret changed or went away while it was copied, for instance through an erasure. The copy is
        // taken back, but only when it is still the version this run wrote.
        if (await store.Read(entry.Destination!) is { } copy && copy.Version == version && VaultLegacyLayout.SameContent(legacy, copy))
        {
            await store.Remove(entry.Destination!);
        }

        return Outcome.ConflictKept;
    }

    async Task Verify(Run run, CancellationToken cancellationToken)
    {
        foreach (var entry in run.Migrated)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await IsSame(entry))
            {
                run.Verified.Add(entry);
            }
            else
            {
                run.Failed++;
            }
        }
    }

    async Task RemoveVerified(Run run, CancellationToken cancellationToken)
    {
        foreach (var entry in run.Verified)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await IsSame(entry))
            {
                await store.Remove(entry.Path);
                run.LegacyRemoved++;
            }
        }
    }

    async Task<bool> IsSame(VaultLegacyEntry entry) =>
        await store.Read(entry.Path) is { } legacy &&
        await store.Read(entry.Destination!) is { } current &&
        VaultLegacyLayout.SameContent(legacy, current);

    sealed record Fence(uint ErasedThrough, IReadOnlyList<string> ErasedKeyFingerprints);

    sealed class Run(bool dryRun)
    {
        public bool DryRun { get; } = dryRun;

        public long Scanned { get; set; }

        public long AlreadyMigrated { get; set; }

        public long Copied { get; set; }

        public long ErasureRecordsCopied { get; set; }

        public long ConflictsKept { get; set; }

        public long AmbiguousForReview { get; set; }

        public long Failed { get; set; }

        public long LegacyRemoved { get; set; }

        public List<VaultLegacyEntry> Migrated { get; } = [];

        public List<VaultLegacyEntry> Verified { get; } = [];

        public void Count(VaultLegacyEntry entry, Outcome outcome)
        {
            switch (outcome)
            {
                case Outcome.AlreadyMigrated:
                    AlreadyMigrated++;
                    Migrated.Add(entry);
                    break;
                case Outcome.Copied when entry.IsErasure:
                    ErasureRecordsCopied++;
                    Migrated.Add(entry);
                    break;
                case Outcome.Copied:
                    Copied++;
                    Migrated.Add(entry);
                    break;
                case Outcome.Ambiguous:
                    AmbiguousForReview++;
                    break;
                default:
                    ConflictsKept++;
                    break;
            }
        }

        public EncryptionKeyMigrationReport Report(bool layoutUnusable, bool complete) => new(
            DryRun,
            layoutUnusable,
            complete,
            Scanned,
            AlreadyMigrated,
            Copied,
            ErasureRecordsCopied,
            ConflictsKept,
            AmbiguousForReview,
            Failed,
            Verified.Count,
            LegacyRemoved);
    }
}
