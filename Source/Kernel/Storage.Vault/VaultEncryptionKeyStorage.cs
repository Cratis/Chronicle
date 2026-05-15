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
/// The Vault token is read from the <c>VAULT_TOKEN</c> environment variable. The Vault address is
/// taken from the <c>connectionDetails</c> constructor parameter.
/// </para>
/// </remarks>
/// <param name="connectionDetails">The Vault server address (for example <c>http://vault:8200</c>).</param>
/// <param name="mountPoint">KV v2 mount point. Defaults to <c>secret</c> when <see langword="null"/> or empty.</param>
public class VaultEncryptionKeyStorage(string connectionDetails, string? mountPoint = null) : IEncryptionKeyStorage
{
    const string DefaultMountPoint = "secret";
    const string PublicKeyField = "publicKey";
    const string PrivateKeyField = "privateKey";

    readonly VaultClient _vaultClient = CreateVaultClient(connectionDetails);
    readonly string _mountPoint = string.IsNullOrEmpty(mountPoint) ? DefaultMountPoint : mountPoint;

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

        var path = BuildPath(eventStore, eventStoreNamespace, identifier, actualRevision);
        var data = new Dictionary<string, object>
        {
            [PublicKeyField] = Convert.ToBase64String(key.Public),
            [PrivateKeyField] = Convert.ToBase64String(key.Private)
        };

        await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, data, mountPoint: _mountPoint);
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

        return await SecretExists(BuildPath(eventStore, eventStoreNamespace, identifier, revision!));
    }

    /// <inheritdoc/>
    public async Task<EncryptionKey> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        EncryptionKeyRevision? targetRevision;
        if (IsLatest(revision))
        {
            targetRevision = await GetHighestRevision(eventStore, eventStoreNamespace, identifier)
                ?? throw new MissingEncryptionKey(identifier);
        }
        else
        {
            targetRevision = revision;
        }

        var path = BuildPath(eventStore, eventStoreNamespace, identifier, targetRevision!);
        try
        {
            var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: _mountPoint);
            if (!secret.Data.Data.TryGetValue(PublicKeyField, out var publicRaw) || publicRaw is null ||
                !secret.Data.Data.TryGetValue(PrivateKeyField, out var privateRaw) || privateRaw is null)
            {
                throw new MissingEncryptionKey(identifier);
            }

            var publicKey = Convert.FromBase64String(publicRaw.ToString()!);
            var privateKey = Convert.FromBase64String(privateRaw.ToString()!);
            return new EncryptionKey(publicKey, privateKey);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            throw new MissingEncryptionKey(identifier);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        if (IsLatest(revision))
        {
            var revisions = await ListRevisions(eventStore, eventStoreNamespace, identifier);
            foreach (var rev in revisions)
            {
                var path = BuildPath(eventStore, eventStoreNamespace, identifier, rev);
                await _vaultClient.V1.Secrets.KeyValue.V2.DeleteMetadataAsync(path, mountPoint: _mountPoint);
            }
        }
        else
        {
            var path = BuildPath(eventStore, eventStoreNamespace, identifier, revision!);
            await _vaultClient.V1.Secrets.KeyValue.V2.DeleteMetadataAsync(path, mountPoint: _mountPoint);
        }
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

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

    string BuildPath(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision revision) =>
        $"{eventStore}/{eventStoreNamespace}/{identifier}/{revision}";

    string BuildDirectoryPath(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier) =>
        $"{eventStore}/{eventStoreNamespace}/{identifier}";

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
        var directory = BuildDirectoryPath(eventStore, eventStoreNamespace, identifier);
        try
        {
            var list = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync(directory, mountPoint: _mountPoint);
            var revisions = new List<EncryptionKeyRevision>();
            foreach (var key in list.Data.Keys)
            {
                if (uint.TryParse(key.TrimEnd('/'), out var parsed))
                {
                    revisions.Add((EncryptionKeyRevision)parsed);
                }
            }

            return revisions;
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
