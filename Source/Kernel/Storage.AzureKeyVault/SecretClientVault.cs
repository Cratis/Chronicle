// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Azure.Security.KeyVault.Secrets;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// Represents an implementation of <see cref="ISecretVault"/> over a <see cref="SecretClient"/>.
/// </summary>
/// <param name="secretClient">The <see cref="SecretClient"/>.</param>
internal class SecretClientVault(SecretClient secretClient) : ISecretVault
{
    const string DeletedButRecoverableCode = "ObjectIsDeletedButRecoverable";

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SecretVersionInfo>> ListLive(CancellationToken cancellationToken)
    {
        var result = new List<SecretVersionInfo>();
        await foreach (var properties in secretClient.GetPropertiesOfSecretsAsync(cancellationToken))
        {
            result.Add(Convert(properties));
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlySet<string>> ListDeleted(CancellationToken cancellationToken)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await foreach (var deleted in secretClient.GetDeletedSecretsAsync(cancellationToken))
        {
            result.Add(deleted.Name);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SecretVersionInfo>> ListVersions(string name, CancellationToken cancellationToken)
    {
        var result = new List<SecretVersionInfo>();
        try
        {
            await foreach (var properties in secretClient.GetPropertiesOfSecretVersionsAsync(name, cancellationToken))
            {
                result.Add(Convert(properties));
            }
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return [];
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<SecretValue?> Read(string name, string? version, CancellationToken cancellationToken)
    {
        try
        {
            var secret = (await secretClient.GetSecretAsync(name, version, cancellationToken)).Value;
            return new(secret.Value, Convert(secret.Properties));
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> Write(string name, string value, CancellationToken cancellationToken)
    {
        try
        {
            return (await secretClient.SetSecretAsync(name, value, cancellationToken)).Value.Properties.Version;
        }
        catch (RequestFailedException exception) when (exception.Status == 409 || exception.ErrorCode == DeletedButRecoverableCode)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<SecretRemoval> Remove(string name, CancellationToken cancellationToken)
    {
        var deletion = await secretClient.StartDeleteSecretAsync(name, cancellationToken);
        await deletion.WaitForCompletionAsync(cancellationToken);
        try
        {
            await secretClient.PurgeDeletedSecretAsync(name, cancellationToken);
            return SecretRemoval.Purged;
        }
        catch (RequestFailedException exception) when (exception.Status == 403)
        {
            return SecretRemoval.DeletedNotPurged;
        }
    }

    static SecretVersionInfo Convert(SecretProperties properties) => new(
        properties.Name,
        properties.Version ?? string.Empty,
        properties.CreatedOn,
        properties.Enabled,
        properties.ExpiresOn,
        properties.NotBefore,
        new Dictionary<string, string>(properties.Tags),
        properties.ContentType);
}
