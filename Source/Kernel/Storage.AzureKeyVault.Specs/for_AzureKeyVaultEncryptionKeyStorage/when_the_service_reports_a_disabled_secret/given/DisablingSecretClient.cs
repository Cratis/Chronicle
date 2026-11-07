// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Azure.Security.KeyVault.Secrets;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_the_service_reports_a_disabled_secret.given;

/// <summary>
/// An in-memory <see cref="SecretClient"/> that keeps every version of a secret and answers a read of a disabled
/// latest version the way the service does: forbidden.
/// </summary>
/// <param name="errorCode">The error code the forbidden answer carries.</param>
/// <param name="listingShowsDisabled">Whether the listing of secrets reports a disabled latest version as disabled, or as enabled.</param>
public class DisablingSecretClient(string errorCode, bool listingShowsDisabled) : SecretClient
{
    static readonly DateTimeOffset _created = DateTimeOffset.UtcNow.AddHours(-1);

    readonly Dictionary<string, List<StoredVersion>> _secrets = [];

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
    /// Adds a version of a secret without counting it as a write.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="value">The value of the version.</param>
    /// <param name="candidate">Whether the version is marked as a provisioning candidate.</param>
    /// <param name="enabled">Whether the version is enabled.</param>
    public void Add(string name, string value, bool candidate = false, bool enabled = true)
    {
        if (!_secrets.TryGetValue(name, out var versions))
        {
            _secrets[name] = versions = [];
        }

        versions.Add(new(value, enabled, candidate));
    }

    /// <summary>
    /// Disables the latest version of a secret.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    public void Disable(string name) => DisableVersion(name, _secrets[name].Count - 1);

    /// <summary>
    /// Disables one version of a secret.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="index">The zero-based position of the version, oldest first.</param>
    public void DisableVersion(string name, int index) => _secrets[name][index] = _secrets[name][index] with { Enabled = false };

    /// <inheritdoc/>
    public override Task<Response<KeyVaultSecret>> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        Writes++;
        Add(name, value);
        return Task.FromResult(Response.FromValue(Create(name, _secrets[name].Count - 1), Substitute.For<Response>()));
    }

    /// <inheritdoc/>
    public override Task<Response<KeyVaultSecret>> SetSecretAsync(KeyVaultSecret secret, CancellationToken cancellationToken = default)
    {
        Writes++;
        Add(secret.Name, secret.Value, ProvisioningCandidateElection.IsCandidate(secret.Properties));
        return Task.FromResult(Response.FromValue(Create(secret.Name, _secrets[secret.Name].Count - 1), Substitute.For<Response>()));
    }

    /// <inheritdoc/>
    public override Task<Response<KeyVaultSecret>> GetSecretAsync(string name, string? version = null, SecretContentType? outContentType = null, CancellationToken cancellationToken = default)
    {
        if (!_secrets.TryGetValue(name, out var versions))
        {
            throw new RequestFailedException(404, "Secret not found", "SecretNotFound", null);
        }

        var index = version is null ? versions.Count - 1 : int.Parse(version);
        if (!versions[index].Enabled)
        {
            throw new RequestFailedException(403, "Operation get is not allowed on a disabled secret.", errorCode, null);
        }

        return Task.FromResult(Response.FromValue(Create(name, index), Substitute.For<Response>()));
    }

    /// <inheritdoc/>
    public override AsyncPageable<SecretProperties> GetPropertiesOfSecretsAsync(CancellationToken cancellationToken = default) =>
        AsyncPageable<SecretProperties>.FromPages([Page<SecretProperties>.FromValues(
            [.. _secrets.Select(pair => Properties(pair.Key, pair.Value.Count - 1, pair.Value[^1].Enabled || !listingShowsDisabled))],
            null,
            Substitute.For<Response>())]);

    /// <inheritdoc/>
    public override AsyncPageable<SecretProperties> GetPropertiesOfSecretVersionsAsync(string name, CancellationToken cancellationToken = default) =>
        AsyncPageable<SecretProperties>.FromPages([Page<SecretProperties>.FromValues(
            [.. (_secrets.TryGetValue(name, out var versions) ? versions : []).Select((version, index) => Properties(name, index, version.Enabled))],
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

    SecretProperties Properties(string name, int index, bool enabled)
    {
        var properties = SecretModelFactory.SecretProperties(name: name, version: index.ToString(), createdOn: _created.AddSeconds(index));
        properties.Enabled = enabled;
        if (_secrets[name][index].Candidate)
        {
            properties.Tags[ProvisioningCandidateElection.CandidateTag] = bool.TrueString;
        }

        return properties;
    }

    KeyVaultSecret Create(string name, int index) =>
        SecretModelFactory.KeyVaultSecret(Properties(name, index, _secrets[name][index].Enabled), _secrets[name][index].Value);

    sealed record StoredVersion(string Value, bool Enabled, bool Candidate);
}
