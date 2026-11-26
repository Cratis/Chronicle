// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator.given;

public class InMemorySecretVault : ISecretVault
{
    readonly Dictionary<string, List<(string Version, string Value, SecretVersionInfo Info)>> _secrets = [];
    readonly HashSet<string> _deleted = [];
    int _counter;

    public bool PurgeRefused { get; set; }

    public bool FailListing { get; set; }

    public bool Has(string name) => _secrets.ContainsKey(name);

    public string? Latest(string name) => _secrets.TryGetValue(name, out var versions) ? versions[^1].Value : null;

    public int VersionCount(string name) => _secrets.TryGetValue(name, out var versions) ? versions.Count : 0;

    public void MarkDeleted(string name) => _deleted.Add(name);

    public void Put(string name, string value, bool? enabled = null, DateTimeOffset? expires = null, Dictionary<string, string>? tags = null)
    {
        var version = (++_counter).ToString();
        var info = new SecretVersionInfo(name, version, DateTimeOffset.UnixEpoch.AddSeconds(_counter), enabled, expires, null, tags ?? [], null);
        if (!_secrets.TryGetValue(name, out var versions))
        {
            _secrets[name] = versions = [];
        }

        versions.Add((version, value, info));
    }

    public Task<IReadOnlyList<SecretVersionInfo>> ListLive(CancellationToken cancellationToken)
    {
        if (FailListing)
        {
            throw new InvalidOperationException("listing failed for secret-name-1");
        }

        return Task.FromResult<IReadOnlyList<SecretVersionInfo>>([.. _secrets.Values.Select(_ => _[^1].Info)]);
    }

    public Task<IReadOnlySet<string>> ListDeleted(CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<string>>(_deleted);

    public Task<IReadOnlyList<SecretVersionInfo>> ListVersions(string name, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SecretVersionInfo>>(_secrets.TryGetValue(name, out var versions) ? [.. versions.Select(_ => _.Info)] : []);

    public Task<SecretValue?> Read(string name, string? version, CancellationToken cancellationToken)
    {
        if (!_secrets.TryGetValue(name, out var versions))
        {
            return Task.FromResult<SecretValue?>(null);
        }

        var found = version is null ? versions[^1] : versions.Find(_ => _.Version == version);
        return Task.FromResult<SecretValue?>(found.Info is null ? null : new SecretValue(found.Value, found.Info));
    }

    public Task<string?> Write(string name, string value, CancellationToken cancellationToken)
    {
        if (_deleted.Contains(name))
        {
            return Task.FromResult<string?>(null);
        }

        Put(name, value);
        return Task.FromResult<string?>(_secrets[name][^1].Version);
    }

    public Task<SecretRemoval> Remove(string name, CancellationToken cancellationToken)
    {
        _secrets.Remove(name);
        if (PurgeRefused)
        {
            _deleted.Add(name);
            return Task.FromResult(SecretRemoval.DeletedNotPurged);
        }

        return Task.FromResult(SecretRemoval.Purged);
    }
}
