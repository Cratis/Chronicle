// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Azure;
using Azure.Security.KeyVault.Secrets;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract.given;

public class VersionedSecretClient : SecretClient
{
    readonly Dictionary<string, List<KeyVaultSecret>> _secrets = [];
    readonly Dictionary<string, List<KeyVaultSecret>> _deleted = [];
    int _sequence;

    public override Uri VaultUri => new("https://contract.vault.azure.net/");

    public Func<Task>? AfterStage { get; set; }
    public Func<Task>? BeforeStage { get; set; }
    public bool FailLegacyWrites { get; set; }
    public bool FailPurge { get; set; }
    public List<(string Name, string Version)> Disabled { get; } = [];
    public List<string> Purged { get; } = [];

    public override Task<Response<KeyVaultSecret>> SetSecretAsync(string name, string value, CancellationToken cancellationToken = default) =>
        SetSecretAsync(new KeyVaultSecret(name, value), cancellationToken);

    public override async Task<Response<KeyVaultSecret>> SetSecretAsync(KeyVaultSecret secret, CancellationToken cancellationToken = default)
    {
        if (FailLegacyWrites && !secret.Properties.Tags.ContainsKey(FencedAzureKeyVaultEncryptionKeyStorage.StagedLifecycleTag)) throw new RequestFailedException(503, "Legacy write unavailable");
        if (secret.Properties.Tags.ContainsKey(FencedAzureKeyVaultEncryptionKeyStorage.StagedLifecycleTag) && BeforeStage is { } before)
        {
            BeforeStage = null;
            await before();
        }

        var properties = SecretModelFactory.SecretProperties(name: secret.Name, version: (++_sequence).ToString(CultureInfo.InvariantCulture), createdOn: DateTimeOffset.UnixEpoch.AddSeconds(_sequence));
        properties.Enabled = true;
        foreach (var tag in secret.Properties.Tags) properties.Tags[tag.Key] = tag.Value;
        var stored = SecretModelFactory.KeyVaultSecret(properties, secret.Value);
        if (!_secrets.TryGetValue(secret.Name, out var versions)) _secrets[secret.Name] = versions = [];
        versions.Add(stored);
        if (secret.Properties.Tags.ContainsKey(FencedAzureKeyVaultEncryptionKeyStorage.StagedLifecycleTag) && AfterStage is { } paused)
        {
            AfterStage = null;
            await paused();
        }

        return Response.FromValue(Clone(stored), Substitute.For<Response>());
    }

    public override Task<Response<KeyVaultSecret>> GetSecretAsync(string name, string? version = null, SecretContentType? outContentType = null, CancellationToken cancellationToken = default)
    {
        var secret = Find(name, version);
        if (secret.Properties.Enabled == false) throw new RequestFailedException(403, "Disabled");
        return Task.FromResult(Response.FromValue(Clone(secret), Substitute.For<Response>()));
    }

    public override Task<Response<SecretProperties>> UpdateSecretPropertiesAsync(SecretProperties properties, CancellationToken cancellationToken = default)
    {
        var secret = Find(properties.Name, properties.Version);
        secret.Properties.Enabled = properties.Enabled;
        if (properties.Enabled == false) Disabled.Add((properties.Name, properties.Version));
        return Task.FromResult(Response.FromValue(Clone(secret).Properties, Substitute.For<Response>()));
    }

    public override AsyncPageable<SecretProperties> GetPropertiesOfSecretsAsync(CancellationToken cancellationToken = default) =>
        Pages(_secrets.Values.Select(_ => Clone(_[^1]).Properties).ToArray());

    public override AsyncPageable<SecretProperties> GetPropertiesOfSecretVersionsAsync(string name, CancellationToken cancellationToken = default) =>
        Pages(_secrets.TryGetValue(name, out var versions) ? versions.Select(_ => Clone(_).Properties).ToArray() : []);

    public override AsyncPageable<DeletedSecret> GetDeletedSecretsAsync(CancellationToken cancellationToken = default) =>
        Pages(_deleted.Values.Select(_ => SecretModelFactory.DeletedSecret(Clone(_[^1]).Properties, _[^1].Value)).ToArray());

    public override Task<DeleteSecretOperation> StartDeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_secrets.Remove(name, out var versions)) throw new RequestFailedException(404, "Missing");
        _deleted[name] = versions;
        return Task.FromResult(Substitute.For<DeleteSecretOperation>());
    }

    public override Task<Response> PurgeDeletedSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (FailPurge) throw new RequestFailedException(403, "Purge denied");
        _deleted.Remove(name);
        Purged.Add(name);
        return Task.FromResult(Substitute.For<Response>());
    }

    static KeyVaultSecret Clone(KeyVaultSecret secret)
    {
        var properties = SecretModelFactory.SecretProperties(name: secret.Name, version: secret.Properties.Version, createdOn: secret.Properties.CreatedOn);
        properties.Enabled = secret.Properties.Enabled;
        foreach (var tag in secret.Properties.Tags) properties.Tags[tag.Key] = tag.Value;
        return SecretModelFactory.KeyVaultSecret(properties, secret.Value);
    }

    static AsyncPageable<T> Pages<T>(T[] values)
        where T : notnull =>
        AsyncPageable<T>.FromPages([Page<T>.FromValues(values, null, Substitute.For<Response>())]);

    KeyVaultSecret Find(string name, string? version)
    {
        if (!_secrets.TryGetValue(name, out var versions)) throw new RequestFailedException(404, "Missing");
        return (version is null ? versions.LastOrDefault() : versions.FirstOrDefault(_ => _.Properties.Version.Equals(version, StringComparison.Ordinal)))
            ?? throw new RequestFailedException(404, "Missing");
    }
}
