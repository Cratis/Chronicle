// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> backed by Azure Key Vault.
/// </summary>
/// <remarks>
/// <para>
/// Encryption keys are stored as secrets in Azure Key Vault. Each revision of a key for a given
/// identifier is stored as a distinct secret so that revisions can be retrieved or deleted independently.
/// </para>
/// <para>
/// Secret names follow the pattern <c language="csharp">chronicle--{eventStore}--{namespace}--{identifier}--{revision}</c>
/// where each component is sanitized to contain only lowercase alphanumeric characters and single hyphens.
/// Double hyphens (<c language="csharp">--</c>) are used as separators and cannot appear within sanitized components.
/// </para>
/// <para>
/// Authentication is performed using the <see cref="Azure.Core.TokenCredential"/> provided to the
/// <see cref="SecretClient"/> constructor parameter.
/// </para>
/// <para>
/// Azure Key Vault has no conditional write, so first-time provisioning cannot be made atomic by the vault itself.
/// Instead every provisioner writes its candidate as a new version of the same secret, tagged as a provisioning
/// candidate, and every reader and provisioner elects the same winner from the versions: the oldest candidate by
/// creation time, tie-broken ordinally on the version identifier. An election is only final once the winner is
/// older than the election's settle time, which is what stops a concurrent write that lands in the same
/// second from being elected after a provisioner has already returned its own key. A secret whose latest version
/// is not a provisioning candidate - one written by <see cref="SaveFor"/> or before elections existed - is served
/// as its latest version, exactly as before.
/// </para>
/// <para>
/// Deleting a key purges it after the soft delete, because crypto-shredding is only an erasure once the key
/// material cannot be recovered. A vault that refuses the purge - purge protection, or a missing purge permission -
/// fails the deletion loudly with <see cref="EncryptionKeyNotPurged"/>.
/// </para>
/// </remarks>
public partial class AzureKeyVaultEncryptionKeyStorage : IEncryptionKeyStorage
{
    const string SecretNamePrefix = "chronicle";
    const string PublicKeyField = "publicKey";
    const string PrivateKeyField = "privateKey";
    const string ErasedThroughField = "erasedThrough";
    const string ErasedKeyFingerprintsField = "erasedKeyFingerprints";
    const string NewKeyAllowedField = "newKeyAllowed";

    /// <summary>
    /// The suffix an erasure is stored under, beside the numbered revisions.
    /// </summary>
    /// <remarks>
    /// Revisions are numbered, so a non-numeric suffix can never collide with one, and the revision enumeration
    /// already skips anything that does not parse as a number - which is what keeps the fence out of it and lets
    /// a deletion of every revision leave the fence standing.
    /// </remarks>
    const string ErasureSuffix = "erasure";

    readonly SecretClient _secretClient;
    readonly ProvisioningCandidateElection _election;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultEncryptionKeyStorage"/> class.
    /// </summary>
    /// <param name="secretClient">The <see cref="SecretClient"/> to use for communicating with Azure Key Vault.</param>
    public AzureKeyVaultEncryptionKeyStorage(SecretClient secretClient)
        : this(secretClient, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultEncryptionKeyStorage"/> class.
    /// </summary>
    /// <param name="secretClient">The <see cref="SecretClient"/> to use for communicating with Azure Key Vault.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> elections are settled against.</param>
    internal AzureKeyVaultEncryptionKeyStorage(SecretClient secretClient, TimeProvider timeProvider)
    {
        _secretClient = secretClient;
        _election = new(secretClient, timeProvider, Deserialize);
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SanitizeRegex { get; }

    /// <inheritdoc/>
    public async Task SaveFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key,
        EncryptionKeyRevision? revision = null)
    {
        var actualRevision = IsLatest(revision)
            ? await GetNextRevision(eventStore, eventStoreNamespace, identifier)
            : revision!;

        (await GetErasureFor(eventStore, eventStoreNamespace, identifier)).EnsureCanSave(identifier, actualRevision, key);

        var name = BuildSecretName(eventStore, eventStoreNamespace, identifier, actualRevision);
        await _secretClient.SetSecretAsync(name, Serialize(key));
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key) =>
        (await GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key)).Key;

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var revision = await GetHighestRevision(eventStore, eventStoreNamespace, identifier);
        if (revision is null)
        {
            return null;
        }

        var key = await TryGetFor(eventStore, eventStoreNamespace, identifier, revision);
        return key is null ? null : new(key, revision);
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        if (await TryGetLatestFor(eventStore, eventStoreNamespace, identifier) is { } existing)
        {
            return existing;
        }

        var revision = (await GetErasureFor(eventStore, eventStoreNamespace, identifier)).RevisionForNewKey(identifier, key);
        var name = BuildSecretName(eventStore, eventStoreNamespace, identifier, revision);
        var candidate = new KeyVaultSecret(name, Serialize(key));
        ProvisioningCandidateElection.MarkAsCandidate(candidate);
        await _secretClient.SetSecretAsync(candidate);

        // Converge on the elected candidate, which is ours unless another provisioner raced us. It is never the
        // candidate unread: if no candidate can be read back, the secret was removed concurrently.
        var winner = await _election.Elect(name) ?? throw new EncryptionKeyProvisioningConflict(identifier, revision);
        return new(winner, revision);
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        if (IsLatest(revision))
        {
            return await GetHighestRevision(eventStore, eventStoreNamespace, identifier) is not null;
        }

        return await SecretExists(BuildSecretName(eventStore, eventStoreNamespace, identifier, revision!));
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey?> TryGetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        var targetRevision = IsLatest(revision)
            ? await GetHighestRevision(eventStore, eventStoreNamespace, identifier)
            : revision;

        if (targetRevision is null)
        {
            return null;
        }

        return await ReadKey(BuildSecretName(eventStore, eventStoreNamespace, identifier, targetRevision));
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null) =>
        await TryGetFor(eventStore, eventStoreNamespace, identifier, revision)
            ?? throw new MissingEncryptionKey(identifier);

    /// <inheritdoc/>
    public async Task DeleteFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        if (IsLatest(revision))
        {
            // Revisions a previous deletion soft-deleted but could not purge are no longer listed as secrets, so
            // they are gathered from the deleted secrets too - repeating an incomplete erasure completes it.
            var revisions = (await ListRevisions(eventStore, eventStoreNamespace, identifier))
                .Concat(await ListDeletedRevisions(eventStore, eventStoreNamespace, identifier))
                .Distinct();
            foreach (var rev in revisions)
            {
                await DeleteAndPurgeSecret(BuildSecretName(eventStore, eventStoreNamespace, identifier, rev));
            }
        }
        else
        {
            await DeleteAndPurgeSecret(BuildSecretName(eventStore, eventStoreNamespace, identifier, revision!));
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        try
        {
            var secret = await _secretClient.GetSecretAsync(BuildErasureSecretName(eventStore, eventStoreNamespace, identifier));
            if (secret.Value.Properties.Enabled == false ||
                JsonSerializer.Deserialize<Dictionary<string, string>>(secret.Value.Value) is not { } data ||
                !data.TryGetValue(ErasedThroughField, out var erasedThrough))
            {
                return null;
            }

            var fingerprints = data.TryGetValue(ErasedKeyFingerprintsField, out var raw)
                ? raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                : [];
            var newKeyAllowed = data.TryGetValue(NewKeyAllowedField, out var allowed) && bool.TryParse(allowed, out var parsed) && parsed;

            return new EncryptionKeyErasure(uint.Parse(erasedThrough), fingerprints, newKeyAllowed);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task RecordErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var present = new List<(EncryptionKeyRevision Revision, EncryptionKey Key)>();
        foreach (var revision in await ListRevisions(eventStore, eventStoreNamespace, identifier))
        {
            if (await TryGetFor(eventStore, eventStoreNamespace, identifier, revision) is { } key)
            {
                present.Add((revision, key));
            }
        }

        var erasure = EncryptionKeyErasure.Covering(await GetErasureFor(eventStore, eventStoreNamespace, identifier), present);
        await WriteErasure(eventStore, eventStoreNamespace, identifier, erasure);
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        if (await GetErasureFor(eventStore, eventStoreNamespace, identifier) is not { } erasure)
        {
            return;
        }

        await WriteErasure(eventStore, eventStoreNamespace, identifier, erasure with { NewKeyAllowed = true });
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    static string Serialize(EncryptionKey key) => JsonSerializer.Serialize(new Dictionary<string, string>
    {
        [PublicKeyField] = Convert.ToBase64String(key.Public),
        [PrivateKeyField] = Convert.ToBase64String(key.Private)
    });

    static EncryptionKey? Deserialize(string value)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(value);
        if (data is null ||
            !data.TryGetValue(PublicKeyField, out var publicBase64) || publicBase64 is null ||
            !data.TryGetValue(PrivateKeyField, out var privateBase64) || privateBase64 is null)
        {
            return null;
        }

        return new EncryptionKey(Convert.FromBase64String(publicBase64), Convert.FromBase64String(privateBase64));
    }

    static string Sanitize(string input) =>
        SanitizeRegex.Replace(input.ToLowerInvariant(), "-").Trim('-');

    string BuildSecretName(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision revision) =>
        $"{SecretNamePrefix}--{Sanitize(eventStore.Value)}--{Sanitize(eventStoreNamespace.Value)}--{Sanitize(identifier.Value)}--{revision.Value}";

    string BuildSecretPrefix(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier) =>
        $"{SecretNamePrefix}--{Sanitize(eventStore.Value)}--{Sanitize(eventStoreNamespace.Value)}--{Sanitize(identifier.Value)}--";

    string BuildErasureSecretName(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier) =>
        $"{BuildSecretPrefix(eventStore, eventStoreNamespace, identifier)}{ErasureSuffix}";

    async Task WriteErasure(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyErasure erasure)
    {
        var value = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            [ErasedThroughField] = erasure.ErasedThrough.Value.ToString(),
            [ErasedKeyFingerprintsField] = string.Join(',', erasure.ErasedKeyFingerprints),
            [NewKeyAllowedField] = erasure.NewKeyAllowed.ToString()
        });

        await _secretClient.SetSecretAsync(BuildErasureSecretName(eventStore, eventStoreNamespace, identifier), value);
    }

    async Task<EncryptionKeyRevision> GetNextRevision(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var highest = await GetHighestRevision(eventStore, eventStoreNamespace, identifier);
        return highest is null ? (EncryptionKeyRevision)1u : highest.Value + 1u;
    }

    async Task<EncryptionKeyRevision?> GetHighestRevision(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var revisions = await ListRevisions(eventStore, eventStoreNamespace, identifier);
        return revisions.Count == 0 ? null : revisions.Max();
    }

    async Task<List<EncryptionKeyRevision>> ListRevisions(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var prefix = BuildSecretPrefix(eventStore, eventStoreNamespace, identifier);
        var revisions = new List<EncryptionKeyRevision>();

        await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync())
        {
            if (secretProperties.Enabled != true)
            {
                continue;
            }

            if (!secretProperties.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var suffix = secretProperties.Name[prefix.Length..];
            if (uint.TryParse(suffix, out var revisionValue))
            {
                revisions.Add(revisionValue);
            }
        }

        return revisions;
    }

    async Task<List<EncryptionKeyRevision>> ListDeletedRevisions(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var prefix = BuildSecretPrefix(eventStore, eventStoreNamespace, identifier);
        var revisions = new List<EncryptionKeyRevision>();

        await foreach (var deleted in _secretClient.GetDeletedSecretsAsync())
        {
            if (deleted.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                uint.TryParse(deleted.Name[prefix.Length..], out var revisionValue))
            {
                revisions.Add(revisionValue);
            }
        }

        return revisions;
    }

    async Task<EncryptionKey?> ReadKey(string name)
    {
        KeyVaultSecret latest;
        try
        {
            latest = (await _secretClient.GetSecretAsync(name)).Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }

        return ProvisioningCandidateElection.IsCandidate(latest.Properties) ? await _election.Elect(name) : Deserialize(latest.Value);
    }

    async Task<bool> SecretExists(string name)
    {
        try
        {
            var response = await _secretClient.GetSecretAsync(name);
            return response.Value.Properties.Enabled != false;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    async Task DeleteAndPurgeSecret(string name)
    {
        try
        {
            var operation = await _secretClient.StartDeleteSecretAsync(name);
            await operation.WaitForCompletionAsync();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Not a live secret - never written, or soft-deleted by an earlier deletion that could not purge it.
        }

        try
        {
            await _secretClient.PurgeDeletedSecretAsync(name);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Nothing soft-deleted to purge - already purged, or a vault without soft delete removed it outright.
        }
        catch (RequestFailedException ex) when (ex.Status == 403)
        {
            throw new EncryptionKeyNotPurged(name, ex);
        }
    }
}
