// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods.Token;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Represents an implementation of <see cref="IEncryptionKeyStorage"/> backed by HashiCorp Vault.
/// </summary>
/// <remarks>
/// <para>
/// Encryption keys are stored as KV v2 secrets in Vault. Each revision of a key for a given
/// identifier is stored at a distinct path so that revisions can be retrieved or deleted independently.
/// </para>
/// <para>
/// The Vault token is read from the <c language="csharp">VAULT_TOKEN</c> environment variable. The Vault address is
/// taken from the <c language="csharp">connectionDetails</c> constructor parameter.
/// </para>
/// <para>
/// Each part of an address becomes one path segment, encoded by <see cref="VaultPathSegments"/>, and every path
/// written by this version lives under <see cref="VaultPathSegments.Root"/>, so every distinct address has its own
/// directory whatever characters it contains. Keys stored by earlier versions are still read, and removed together
/// with the key, for the addresses <see cref="VaultPathSegments.LegacyDirectory"/> accepts: those made of characters
/// that can belong to one address only. Earlier paths are never written.
/// </para>
/// </remarks>
public class VaultEncryptionKeyStorage : IEncryptionKeyStorage
{
    const string DefaultMountPoint = "secret";
    const string PublicKeyField = "publicKey";
    const string PrivateKeyField = "privateKey";
    const string ErasedThroughField = "erasedThrough";
    const string ErasedKeyFingerprintsField = "erasedKeyFingerprints";
    const string NewKeyAllowedField = "newKeyAllowed";
    const string LayoutField = "layout";

    /// <summary>
    /// The error Vault answers a check-and-set write with when the path already has a version.
    /// </summary>
    const string CheckAndSetMismatch = "check-and-set";

    readonly IVaultClient _vaultClient;
    readonly string _mountPoint;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultEncryptionKeyStorage"/> class.
    /// </summary>
    /// <param name="connectionDetails">The Vault server address (for example <c language="csharp">http://vault:8200</c>).</param>
    /// <param name="mountPoint">KV v2 mount point. Defaults to <c language="csharp">secret</c> when <see langword="null"/> or empty.</param>
    public VaultEncryptionKeyStorage(string connectionDetails, string? mountPoint = null)
        : this(CreateVaultClient(connectionDetails), mountPoint)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultEncryptionKeyStorage"/> class over an existing client.
    /// </summary>
    /// <param name="vaultClient">The <see cref="IVaultClient"/> to talk to Vault through.</param>
    /// <param name="mountPoint">KV v2 mount point. Defaults to <c language="csharp">secret</c> when <see langword="null"/> or empty.</param>
    internal VaultEncryptionKeyStorage(IVaultClient vaultClient, string? mountPoint = null)
    {
        _vaultClient = vaultClient;
        _mountPoint = string.IsNullOrEmpty(mountPoint) ? DefaultMountPoint : mountPoint;
    }

    /// <inheritdoc/>
    public async Task SaveFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key,
        EncryptionKeyRevision? revision = null)
    {
        var locations = VaultKeyLocations.For(eventStore, eventStoreNamespace, identifier);
        var actualRevision = IsLatest(revision)
            ? await GetNextRevision(locations)
            : revision!;

        (await GetErasureFor(locations)).EnsureCanSave(identifier, actualRevision, key);

        await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(locations.Path(actualRevision), KeyData(key), mountPoint: _mountPoint);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Race-safe across silos: the key is written with a KV v2 check-and-set of zero, which Vault only accepts when
    /// the path has no version yet. A provisioner that loses the write converges on the key that won.
    /// </remarks>
    public async Task<EncryptionKey> GetOrAddFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKey key) =>
        (await GetOrAddWithRevisionFor(eventStore, eventStoreNamespace, identifier, key)).Key;

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision?> TryGetLatestFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        await TryGetLatest(VaultKeyLocations.For(eventStore, eventStoreNamespace, identifier));

    /// <inheritdoc/>
    public async Task<EncryptionKeyAtRevision> GetOrAddWithRevisionFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKey key)
    {
        var locations = VaultKeyLocations.For(eventStore, eventStoreNamespace, identifier);
        if (await TryGetLatest(locations) is { } existing)
        {
            return existing;
        }

        var revision = (await GetErasureFor(locations)).RevisionForNewKey(identifier, key);
        try
        {
            await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(locations.Path(revision), KeyData(key), checkAndSet: 0, mountPoint: _mountPoint);
            return new(key, revision);
        }
        catch (VaultApiException ex) when (IsCheckAndSetMismatch(ex))
        {
            // Another provisioner wrote this revision first. Converge on its key; never hand back the candidate,
            // which was not written. A winner that cannot be read back was removed concurrently.
            var winner = await ReadKey(locations.Path(revision))
                ?? throw new EncryptionKeyProvisioningConflict(identifier, revision);
            return new(winner, revision);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> HasFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        var locations = VaultKeyLocations.For(eventStore, eventStoreNamespace, identifier);
        if (IsLatest(revision))
        {
            return (await ListRevisions(locations)).Count > 0;
        }

        return await SecretExists(locations.Path(revision!)) ||
            (locations.LegacyPath(revision!) is { } legacyPath && await SecretExists(legacyPath));
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey?> TryGetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        var locations = VaultKeyLocations.For(eventStore, eventStoreNamespace, identifier);
        if (IsLatest(revision))
        {
            return (await TryGetLatest(locations))?.Key;
        }

        return await ReadKey(locations.Path(revision!)) ??
            (locations.LegacyPath(revision!) is { } legacyPath ? await ReadKey(legacyPath) : null);
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
        var locations = VaultKeyLocations.For(eventStore, eventStoreNamespace, identifier);
        if (IsLatest(revision))
        {
            foreach (var path in (await ListRevisions(locations)).Values)
            {
                await _vaultClient.V1.Secrets.KeyValue.V2.DeleteMetadataAsync(path, mountPoint: _mountPoint);
            }

            // A revision present under both directories is only listed under the current one.
            if (locations.LegacyDirectory is not null)
            {
                foreach (var legacyRevision in await ListRevisionsIn(locations.LegacyDirectory))
                {
                    await _vaultClient.V1.Secrets.KeyValue.V2.DeleteMetadataAsync($"{locations.LegacyDirectory}/{legacyRevision}", mountPoint: _mountPoint);
                }
            }
        }
        else
        {
            await _vaultClient.V1.Secrets.KeyValue.V2.DeleteMetadataAsync(locations.Path(revision!), mountPoint: _mountPoint);
            if (locations.LegacyPath(revision!) is { } legacyPath)
            {
                await _vaultClient.V1.Secrets.KeyValue.V2.DeleteMetadataAsync(legacyPath, mountPoint: _mountPoint);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier) =>
        await GetErasureFor(VaultKeyLocations.For(eventStore, eventStoreNamespace, identifier));

    /// <inheritdoc/>
    public async Task RecordErasureFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier)
    {
        var locations = VaultKeyLocations.For(eventStore, eventStoreNamespace, identifier);
        var present = new List<(EncryptionKeyRevision Revision, EncryptionKey Key)>();
        foreach (var (revision, path) in await ListRevisions(locations))
        {
            if (await ReadKey(path) is { } key)
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
        var locations = VaultKeyLocations.For(eventStore, eventStoreNamespace, identifier);
        if (await GetErasureFor(locations) is not { } erasure)
        {
            return;
        }

        await WriteErasure(locations, erasure with { NewKeyAllowed = true });
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

    static Dictionary<string, object> KeyData(EncryptionKey key) => new()
    {
        [PublicKeyField] = Convert.ToBase64String(key.Public),
        [PrivateKeyField] = Convert.ToBase64String(key.Private),
        [LayoutField] = VaultPathSegments.Root
    };

    static bool IsCheckAndSetMismatch(VaultApiException exception) =>
        exception.HttpStatusCode == HttpStatusCode.BadRequest &&
        ((exception.ApiErrors?.Any(error => error.Contains(CheckAndSetMismatch, StringComparison.OrdinalIgnoreCase)) ?? false) ||
         exception.Message.Contains(CheckAndSetMismatch, StringComparison.OrdinalIgnoreCase));

    static VaultClient CreateVaultClient(string address)
    {
        var token = Environment.GetEnvironmentVariable("VAULT_TOKEN");
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException(
                "VAULT_TOKEN environment variable is required for HashiCorp Vault authentication. " +
                "Set this variable to a valid Vault token before starting Chronicle.");
        }

        var authMethod = new TokenAuthMethodInfo(token);
        var settings = new VaultClientSettings(address, authMethod);
        return new VaultClient(settings);
    }

    async Task<EncryptionKeyAtRevision?> TryGetLatest(VaultKeyLocations locations)
    {
        var revisions = await ListRevisions(locations);
        if (revisions.Count == 0)
        {
            return null;
        }

        var (revision, path) = revisions.MaxBy(pair => pair.Key.Value);
        var key = await ReadKey(path);
        return key is null ? null : new(key, revision);
    }

    async Task<EncryptionKey?> ReadKey(string path)
    {
        try
        {
            var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: _mountPoint);
            if (!secret.Data.Data.TryGetValue(PublicKeyField, out var publicRaw) || publicRaw is null ||
                !secret.Data.Data.TryGetValue(PrivateKeyField, out var privateRaw) || privateRaw is null)
            {
                return null;
            }

            var publicKey = Convert.FromBase64String(publicRaw.ToString()!);
            var privateKey = Convert.FromBase64String(privateRaw.ToString()!);
            return new EncryptionKey(publicKey, privateKey);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    async Task<EncryptionKeyErasure?> GetErasureFor(VaultKeyLocations locations)
    {
        var (found, erasure) = await ReadErasure(locations.ErasurePath);
        if (found || locations.LegacyErasurePath is not { } legacyPath)
        {
            return erasure;
        }

        return (await ReadErasure(legacyPath)).Erasure;
    }

    async Task<(bool Found, EncryptionKeyErasure? Erasure)> ReadErasure(string path)
    {
        try
        {
            var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: _mountPoint);
            if (!secret.Data.Data.TryGetValue(ErasedThroughField, out var erasedThrough) || erasedThrough is null)
            {
                return (true, null);
            }

            var fingerprints = secret.Data.Data.TryGetValue(ErasedKeyFingerprintsField, out var raw) && raw is not null
                ? raw.ToString()!.Split(',', StringSplitOptions.RemoveEmptyEntries)
                : [];
            var newKeyAllowed = secret.Data.Data.TryGetValue(NewKeyAllowedField, out var allowed) && allowed is not null &&
                bool.TryParse(allowed.ToString(), out var parsed) && parsed;

            return (true, new EncryptionKeyErasure(uint.Parse(erasedThrough.ToString()!), fingerprints, newKeyAllowed));
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return (false, null);
        }
    }

    async Task WriteErasure(VaultKeyLocations locations, EncryptionKeyErasure erasure)
    {
        var data = new Dictionary<string, object>
        {
            [ErasedThroughField] = erasure.ErasedThrough.Value.ToString(),
            [ErasedKeyFingerprintsField] = string.Join(',', erasure.ErasedKeyFingerprints),
            [NewKeyAllowedField] = erasure.NewKeyAllowed.ToString(),
            [LayoutField] = VaultPathSegments.Root
        };

        await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(locations.ErasurePath, data, mountPoint: _mountPoint);
    }

    async Task<EncryptionKeyRevision> GetNextRevision(VaultKeyLocations locations)
    {
        var revisions = await ListRevisions(locations);
        return revisions.Count == 0 ? (EncryptionKeyRevision)1u : revisions.Keys.Max()!.Value + 1u;
    }

    /// <summary>
    /// Lists the revisions present, mapped to the path each is read from: the current path when a revision exists
    /// under both directories, otherwise the earlier one.
    /// </summary>
    /// <param name="locations">The <see cref="VaultKeyLocations"/> of the address.</param>
    /// <returns>The revisions and their paths.</returns>
    async Task<Dictionary<EncryptionKeyRevision, string>> ListRevisions(VaultKeyLocations locations)
    {
        var revisions = new Dictionary<EncryptionKeyRevision, string>();
        foreach (var revision in await ListRevisionsIn(locations.Directory))
        {
            revisions[revision] = $"{locations.Directory}/{revision}";
        }

        if (locations.LegacyDirectory is not null)
        {
            foreach (var revision in await ListRevisionsIn(locations.LegacyDirectory))
            {
                revisions.TryAdd(revision, $"{locations.LegacyDirectory}/{revision}");
            }
        }

        return revisions;
    }

    async Task<List<EncryptionKeyRevision>> ListRevisionsIn(string directory)
    {
        try
        {
            var list = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(directory, mountPoint: _mountPoint);
            return list.Data.Keys
                .Where(key => !key.EndsWith('/') && uint.TryParse(key, out _))
                .Select(key => (EncryptionKeyRevision)uint.Parse(key))
                .ToList();
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }
    }

    async Task<bool> SecretExists(string path)
    {
        try
        {
            await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: _mountPoint);
            return true;
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
