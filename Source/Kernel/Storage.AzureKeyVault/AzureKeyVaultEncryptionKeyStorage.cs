// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
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
/// Secret names follow the pattern <c language="csharp">chronicle-k1--{hash}--{revision}</c> where the hash is the SHA-256 of the
/// event store, the namespace and the identifier - see <see cref="SecretNames"/>. Every distinct address therefore has
/// its own secrets, and the names carry no subject data.
/// </para>
/// <para>
/// Keys that were stored under the earlier <c language="csharp">chronicle--{eventStore}--{namespace}--{identifier}--{revision}</c>
/// names are still read, and removed together with the key, for the addresses whose components are made of lowercase letters, digits and
/// single hyphens (plus the identifiers Chronicle builds for encrypted values). Earlier names are never written.
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
public class AzureKeyVaultEncryptionKeyStorage : IEncryptionKeyStorage
{
    const string PublicKeyField = "publicKey";
    const string PrivateKeyField = "privateKey";
    const string ErasedThroughField = "erasedThrough";
    const string ErasedKeyFingerprintsField = "erasedKeyFingerprints";
    const string NewKeyAllowedField = "newKeyAllowed";

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

    /// <inheritdoc/>
    public async Task SaveFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key,
        EncryptionKeyRevision? revision = null)
    {
        var locations = SecretLocations.For(eventStore, eventStoreNamespace, identifier);
        var actualRevision = IsLatest(revision)
            ? await GetNextRevision(locations)
            : revision!;

        (await GetErasureFor(locations)).EnsureCanSave(identifier, actualRevision, key);

        await _secretClient.SetSecretAsync(locations.Name(actualRevision), Serialize(key));
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetOrAddFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key) =>
        (await GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key)).Key;

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        await TryGetLatest(SecretLocations.For(eventStore, eventStoreNamespace, identifier));

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        var locations = SecretLocations.For(eventStore, eventStoreNamespace, identifier);
        if (await TryGetLatest(locations) is { } existing)
        {
            return existing;
        }

        var revision = (await GetErasureFor(locations)).RevisionForNewKey(identifier, key);
        var name = locations.Name(revision);
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
        var locations = SecretLocations.For(eventStore, eventStoreNamespace, identifier);
        if (IsLatest(revision))
        {
            return await GetHighestRevision(locations) is not null;
        }

        return await SecretExists(locations.Name(revision!)) ||
            (locations.LegacyName(revision!) is { } legacyName && await SecretExists(legacyName));
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey?> TryGetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        var locations = SecretLocations.For(eventStore, eventStoreNamespace, identifier);
        if (IsLatest(revision))
        {
            return (await TryGetLatest(locations))?.Key;
        }

        return await ReadKey(locations.Name(revision!)) ??
            (locations.LegacyName(revision!) is { } legacyName ? await ReadKey(legacyName) : null);
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
        var locations = SecretLocations.For(eventStore, eventStoreNamespace, identifier);
        if (IsLatest(revision))
        {
            // Revisions a previous deletion soft-deleted but could not purge are no longer listed as secrets, so
            // they are gathered from the deleted secrets too - repeating an incomplete erasure completes it.
            var names = (await ListSecrets(locations)).Select(secret => secret.Name)
                .Concat((await ListDeletedSecrets(locations)).Select(secret => secret.Name))
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
            {
                await DeleteAndPurgeSecret(name);
            }
        }
        else
        {
            await DeleteAndPurgeSecret(locations.Name(revision!));
            if (locations.LegacyName(revision!) is { } legacyName)
            {
                await DeleteAndPurgeSecret(legacyName);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier) =>
        await GetErasureFor(SecretLocations.For(eventStore, eventStoreNamespace, identifier));

    /// <inheritdoc/>
    public async Task RecordErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var locations = SecretLocations.For(eventStore, eventStoreNamespace, identifier);
        var present = new List<(EncryptionKeyRevision Revision, EncryptionKey Key)>();
        foreach (var (revision, name) in await ListRevisions(locations))
        {
            if (await ReadKey(name) is { } key)
            {
                present.Add((revision, key));
            }
        }

        var erasure = EncryptionKeyErasure.Covering(await GetErasureFor(locations), present);
        await WriteErasure(locations, erasure);
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var locations = SecretLocations.For(eventStore, eventStoreNamespace, identifier);
        if (await GetErasureFor(locations) is not { } erasure)
        {
            return;
        }

        await WriteErasure(locations, erasure with { NewKeyAllowed = true });
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

    /// <summary>
    /// Matches a secret name against the current prefix and, when the address uses earlier names, the earlier prefix.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="locations">The <see cref="SecretLocations"/> of the address.</param>
    /// <param name="revision">The revision the name carries.</param>
    /// <param name="isCurrent">Whether the name uses the current prefix.</param>
    /// <returns>True when the name belongs to the address.</returns>
    static bool TryParseRevision(string name, SecretLocations locations, out uint revision, out bool isCurrent)
    {
        isCurrent = name.StartsWith(locations.Prefix, StringComparison.OrdinalIgnoreCase);
        var prefix = isCurrent ? locations.Prefix : locations.LegacyPrefix;
        if (prefix is not null && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(name[prefix.Length..], out revision);
        }

        revision = 0;
        return false;
    }

    async Task<EncryptionKeyAtRevision?> TryGetLatest(SecretLocations locations)
    {
        var revisions = await ListRevisions(locations);
        if (revisions.Count == 0)
        {
            return null;
        }

        var (revision, name) = revisions.MaxBy(pair => pair.Key.Value);
        var key = await ReadKey(name);
        return key is null ? null : new(key, revision);
    }

    async Task<EncryptionKeyErasure?> GetErasureFor(SecretLocations locations)
    {
        var (found, erasure) = await ReadErasure(locations.ErasureName);
        if (found || locations.LegacyErasureName is not { } legacyName)
        {
            return erasure;
        }

        return (await ReadErasure(legacyName)).Erasure;
    }

    async Task<(bool Found, EncryptionKeyErasure? Erasure)> ReadErasure(string name)
    {
        try
        {
            var secret = await _secretClient.GetSecretAsync(name);
            if (secret.Value.Properties.Enabled == false ||
                JsonSerializer.Deserialize<Dictionary<string, string>>(secret.Value.Value) is not { } data ||
                !data.TryGetValue(ErasedThroughField, out var erasedThrough))
            {
                return (true, null);
            }

            var fingerprints = data.TryGetValue(ErasedKeyFingerprintsField, out var raw)
                ? raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                : [];
            var newKeyAllowed = data.TryGetValue(NewKeyAllowedField, out var allowed) && bool.TryParse(allowed, out var parsed) && parsed;

            return (true, new EncryptionKeyErasure(uint.Parse(erasedThrough), fingerprints, newKeyAllowed));
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return (false, null);
        }
    }

    async Task WriteErasure(SecretLocations locations, EncryptionKeyErasure erasure)
    {
        var value = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            [ErasedThroughField] = erasure.ErasedThrough.Value.ToString(),
            [ErasedKeyFingerprintsField] = string.Join(',', erasure.ErasedKeyFingerprints),
            [NewKeyAllowedField] = erasure.NewKeyAllowed.ToString()
        });

        await _secretClient.SetSecretAsync(locations.ErasureName, value);
    }

    async Task<EncryptionKeyRevision> GetNextRevision(SecretLocations locations)
    {
        var highest = await GetHighestRevision(locations);
        return highest is null ? (EncryptionKeyRevision)1u : highest.Value + 1u;
    }

    async Task<EncryptionKeyRevision?> GetHighestRevision(SecretLocations locations)
    {
        var revisions = await ListRevisions(locations);
        return revisions.Count == 0 ? null : revisions.Keys.Max();
    }

    /// <summary>
    /// Lists the revisions present, mapped to the name each is read from: the current name when a revision exists
    /// under both, otherwise the earlier one.
    /// </summary>
    /// <param name="locations">The <see cref="SecretLocations"/> of the address.</param>
    /// <returns>The revisions and their names.</returns>
    async Task<Dictionary<EncryptionKeyRevision, string>> ListRevisions(SecretLocations locations)
    {
        var revisions = new Dictionary<EncryptionKeyRevision, string>();
        foreach (var secret in await ListSecrets(locations))
        {
            if (secret.IsCurrent)
            {
                revisions[secret.Revision] = secret.Name;
            }
            else
            {
                revisions.TryAdd(secret.Revision, secret.Name);
            }
        }

        return revisions;
    }

    async Task<List<ListedSecret>> ListSecrets(SecretLocations locations)
    {
        var secrets = new List<ListedSecret>();

        await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync())
        {
            if (secretProperties.Enabled == true &&
                TryParseRevision(secretProperties.Name, locations, out var revision, out var isCurrent))
            {
                secrets.Add(new(revision, secretProperties.Name, isCurrent));
            }
        }

        return secrets;
    }

    async Task<List<ListedSecret>> ListDeletedSecrets(SecretLocations locations)
    {
        var secrets = new List<ListedSecret>();

        await foreach (var deleted in _secretClient.GetDeletedSecretsAsync())
        {
            if (TryParseRevision(deleted.Name, locations, out var revision, out var isCurrent))
            {
                secrets.Add(new(revision, deleted.Name, isCurrent));
            }
        }

        return secrets;
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

    readonly record struct ListedSecret(EncryptionKeyRevision Revision, string Name, bool IsCurrent);
}
