// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator.given;

/// <summary>
/// A KV v2 mount in memory, with hooks to fail or to act like another writer.
/// </summary>
public class InMemoryVaultSecretStore : IVaultSecretStore
{
    readonly Dictionary<string, (int Version, Dictionary<string, string> Data)> _secrets = new(StringComparer.Ordinal);

    public int Writes { get; private set; }

    public int Removes { get; private set; }

    public int? FailOnWrite { get; set; }

    public Exception? FailEverything { get; set; }

    public Action<string>? BeforeWrite { get; set; }

    public Action<string>? AfterWrite { get; set; }

    public IReadOnlyCollection<string> Paths => _secrets.Keys;

    public void Put(string path, IReadOnlyDictionary<string, string> data)
    {
        var version = _secrets.TryGetValue(path, out var existing) ? existing.Version + 1 : 1;
        _secrets[path] = (version, new(data, StringComparer.Ordinal));
    }

    public void Delete(string path) => _secrets.Remove(path);

    public bool Has(string path) => _secrets.ContainsKey(path);

    public IReadOnlyDictionary<string, string> DataOf(string path) => _secrets[path].Data;

    Task<IReadOnlyList<string>> IVaultSecretStore.List(string path)
    {
        ThrowIfFailing();
        var prefix = path.Length == 0 ? string.Empty : $"{path}/";
        IReadOnlyList<string> entries = _secrets.Keys
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(key => key[prefix.Length..])
            .Select(rest => rest.Contains('/') ? $"{rest[..rest.IndexOf('/')]}/" : rest)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(entries);
    }

    Task<VaultSecret?> IVaultSecretStore.Read(string path)
    {
        ThrowIfFailing();
        return Task.FromResult(_secrets.TryGetValue(path, out var secret)
            ? new VaultSecret(secret.Version, new Dictionary<string, string>(secret.Data, StringComparer.Ordinal))
            : null);
    }

    Task<int?> IVaultSecretStore.WriteIfAbsent(string path, IReadOnlyDictionary<string, string> data)
    {
        ThrowIfFailing();
        BeforeWrite?.Invoke(path);
        Writes++;
        if (FailOnWrite == Writes)
        {
            throw new InvalidOperationException($"write failed at {path}");
        }

        if (_secrets.ContainsKey(path))
        {
            return Task.FromResult<int?>(null);
        }

        Put(path, data);
        AfterWrite?.Invoke(path);
        return Task.FromResult<int?>(_secrets[path].Version);
    }

    Task IVaultSecretStore.Remove(string path)
    {
        ThrowIfFailing();
        Removes++;
        _secrets.Remove(path);
        return Task.CompletedTask;
    }

    void ThrowIfFailing()
    {
        if (FailEverything is not null)
        {
            throw FailEverything;
        }
    }
}
