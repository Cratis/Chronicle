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
/// Secret names follow the pattern <c>chronicle--{eventStore}--{namespace}--{identifier}--{revision}</c>
/// where each component is sanitized to contain only lowercase alphanumeric characters and single hyphens.
/// Double hyphens (<c>--</c>) are used as separators and cannot appear within sanitized components.
/// </para>
/// <para>
/// Authentication is performed using the <see cref="Azure.Core.TokenCredential"/> provided to the
/// <see cref="SecretClient"/> constructor parameter.
/// </para>
/// </remarks>
/// <param name="secretClient">The <see cref="SecretClient"/> to use for communicating with Azure Key Vault.</param>
public partial class AzureKeyVaultEncryptionKeyStorage(SecretClient secretClient) : IEncryptionKeyStorage
{
    const string SecretNamePrefix = "chronicle";
    const string PublicKeyField = "publicKey";
    const string PrivateKeyField = "privateKey";

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

        var name = BuildSecretName(eventStore, eventStoreNamespace, identifier, actualRevision);
        var value = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            [PublicKeyField] = Convert.ToBase64String(key.Public),
            [PrivateKeyField] = Convert.ToBase64String(key.Private)
        });

        await secretClient.SetSecretAsync(name, value);
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
    public async Task<EncryptionKey> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier identifier,
        EncryptionKeyRevision? revision = null)
    {
        var targetRevision = IsLatest(revision)
            ? await GetHighestRevision(eventStore, eventStoreNamespace, identifier)
                ?? throw new MissingEncryptionKey(identifier)
            : revision;

        var name = BuildSecretName(eventStore, eventStoreNamespace, identifier, targetRevision!);
        try
        {
            var secret = await secretClient.GetSecretAsync(name);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(secret.Value.Value);
            if (data is null ||
                !data.TryGetValue(PublicKeyField, out var publicBase64) || publicBase64 is null ||
                !data.TryGetValue(PrivateKeyField, out var privateBase64) || privateBase64 is null)
            {
                throw new MissingEncryptionKey(identifier);
            }

            return new EncryptionKey(Convert.FromBase64String(publicBase64), Convert.FromBase64String(privateBase64));
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
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
                await DeleteSecret(BuildSecretName(eventStore, eventStoreNamespace, identifier, rev));
            }
        }
        else
        {
            await DeleteSecret(BuildSecretName(eventStore, eventStoreNamespace, identifier, revision!));
        }
    }

    static bool IsLatest(EncryptionKeyRevision? revision) => revision is null || revision == EncryptionKeyRevision.Latest;

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

        await foreach (var secretProperties in secretClient.GetPropertiesOfSecretsAsync())
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

    async Task<bool> SecretExists(string name)
    {
        try
        {
            var response = await secretClient.GetSecretAsync(name);
            return response.Value.Properties.Enabled != false;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
    }

    async Task DeleteSecret(string name)
    {
        try
        {
            var operation = await secretClient.StartDeleteSecretAsync(name);
            await operation.WaitForCompletionAsync();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Secret does not exist — nothing to delete.
        }
    }
}
