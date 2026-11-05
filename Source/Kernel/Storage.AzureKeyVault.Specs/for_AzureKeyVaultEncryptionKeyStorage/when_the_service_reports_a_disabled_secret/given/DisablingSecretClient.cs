// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Azure.Security.KeyVault.Secrets;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_the_service_reports_a_disabled_secret.given;

/// <summary>
/// An in-memory <see cref="SecretClient"/> that answers a read of a disabled secret the way the service does: forbidden.
/// </summary>
/// <param name="errorCode">The error code the forbidden answer carries.</param>
/// <param name="listingShowsDisabled">Whether the listings report a disabled secret as disabled, or as enabled.</param>
public class DisablingSecretClient(string errorCode, bool listingShowsDisabled) : SecretClient
{
    readonly Dictionary<string, List<(string Value, bool Enabled)>> _secrets = [];

    /// <summary>
    /// Gets the number of secret versions written.
    /// </summary>
    public int Writes { get; private set; }

    /// <summary>
    /// Gets the names of the secrets that were purged.
    /// </summary>
    public List<string> Purged { get; } = [];

    /// <summary>
    /// Gets the names of the secrets that are live.
    /// </summary>
    public IEnumerable<string> LiveNames => _secrets.Keys;

    /// <summary>
    /// Disables the latest version of a secret.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    public void Disable(string name)
    {
        var versions = _secrets[name];
        versions[^1] = (versions[^1].Value, false);
    }

    /// <inheritdoc/>
    public override Task<Response<KeyVaultSecret>> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        Writes++;
        if (!_secrets.TryGetValue(name, out var versions))
        {
            _secrets[name] = versions = [];
        }

        versions.Add((value, true));
        return Task.FromResult(Response.FromValue(Create(name, value, true), Substitute.For<Response>()));
    }

    /// <inheritdoc/>
    public override Task<Response<KeyVaultSecret>> SetSecretAsync(KeyVaultSecret secret, CancellationToken cancellationToken = default) =>
        SetSecretAsync(secret.Name, secret.Value, cancellationToken);

    /// <inheritdoc/>
    public override Task<Response<KeyVaultSecret>> GetSecretAsync(string name, string? version = null, SecretContentType? outContentType = null, CancellationToken cancellationToken = default)
    {
        if (!_secrets.TryGetValue(name, out var versions))
        {
            throw new RequestFailedException(404, "Secret not found", "SecretNotFound", null);
        }

        var latest = versions[^1];
        if (!latest.Enabled)
        {
            throw new RequestFailedException(403, "Operation get is not allowed on a disabled secret.", errorCode, null);
        }

        return Task.FromResult(Response.FromValue(Create(name, latest.Value, true), Substitute.For<Response>()));
    }

    /// <inheritdoc/>
    public override AsyncPageable<SecretProperties> GetPropertiesOfSecretsAsync(CancellationToken cancellationToken = default) =>
        AsyncPageable<SecretProperties>.FromPages([Page<SecretProperties>.FromValues(
            [.. _secrets.Select(pair => Properties(pair.Key, "0", pair.Value[^1].Enabled || !listingShowsDisabled))],
            null,
            Substitute.For<Response>())]);

    /// <inheritdoc/>
    public override AsyncPageable<SecretProperties> GetPropertiesOfSecretVersionsAsync(string name, CancellationToken cancellationToken = default) =>
        AsyncPageable<SecretProperties>.FromPages([Page<SecretProperties>.FromValues(
            [.. (_secrets.TryGetValue(name, out var versions) ? versions : []).Select((version, index) => Properties(name, index.ToString(), version.Enabled))],
            null,
            Substitute.For<Response>())]);

    /// <inheritdoc/>
    public override AsyncPageable<DeletedSecret> GetDeletedSecretsAsync(CancellationToken cancellationToken = default) =>
        AsyncPageable<DeletedSecret>.FromPages([Page<DeletedSecret>.FromValues([], null, Substitute.For<Response>())]);

    /// <inheritdoc/>
    public override Task<DeleteSecretOperation> StartDeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_secrets.Remove(name))
        {
            throw new RequestFailedException(404, "Secret not found", "SecretNotFound", null);
        }

        return Task.FromResult(Substitute.For<DeleteSecretOperation>());
    }

    /// <inheritdoc/>
    public override Task<Response> PurgeDeletedSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        Purged.Add(name);
        return Task.FromResult(Substitute.For<Response>());
    }

    static KeyVaultSecret Create(string name, string value, bool enabled)
    {
        return SecretModelFactory.KeyVaultSecret(Properties(name, "0", enabled), value);
    }

    static SecretProperties Properties(string name, string version, bool enabled)
    {
        var properties = SecretModelFactory.SecretProperties(name: name, version: version);
        properties.Enabled = enabled;
        return properties;
    }
}
