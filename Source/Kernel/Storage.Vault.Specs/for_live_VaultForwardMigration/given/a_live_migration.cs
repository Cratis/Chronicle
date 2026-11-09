// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration.given;

public class a_live_migration(LiveVaultFixture fixture) : Specification
{
    protected const string Legacy = "store/default/customer-42/1";
    protected const string Current = "k1/store/default/customer-42/1";
    protected string _mount = default!;
    protected HttpClient _http = default!;
    private protected IVaultSecretStore _store = default!;
    private protected VaultKeyStoreUpgradeCheck _check = default!;
    protected IEncryptionKeyStoreMigrator _migrator = default!;
    protected VaultEncryptionKeyStorage _storage = default!;

    async Task Establish()
    {
        _mount = $"proof-{Guid.NewGuid():N}";
        _http = fixture.CreateHttpClient();
        await Post($"sys/mounts/{_mount}", new { type = "kv", options = new { version = "2" } });
        var client = fixture.CreateClient();
        _store = new VaultSecretStore(client, _mount);
        (await _store.IsKv2()).ShouldBeTrue();
        _check = new VaultKeyStoreUpgradeCheck(_store);
        _migrator = new VaultKeyStoreMigrator(_store, NullLogger<VaultKeyStoreMigrator>.Instance);
        _storage = new VaultEncryptionKeyStorage(client, _mount);
    }

    void Destroy() => _http.Dispose();

    protected static Dictionary<string, string> Key(byte value = 1) => new()
    {
        ["publicKey"] = Convert.ToBase64String([value, 1, 2]),
        ["privateKey"] = Convert.ToBase64String([value, 3, 4])
    };

    protected static Dictionary<string, string> WithLayout(Dictionary<string, string> data) => new(data) { ["layout"] = "k1" };

    protected Task Put(string path, Dictionary<string, string>? data = null) => Post($"{_mount}/data/{string.Join('/', path.Split('/').Select(Uri.EscapeDataString))}", new { data = data ?? Key() });

    protected Task Marker() => Put(VaultPathSegments.LayoutMarker, new() { ["format"] = "k1" });

    protected Task DeleteVersions(string path, params int[] versions) => Post($"{_mount}/delete/{path}", new { versions });

    protected Task DestroyVersions(string path, params int[] versions) => Post($"{_mount}/destroy/{path}", new { versions });

    protected Task<JsonNode?> Metadata(string path) => Get($"{_mount}/metadata/{path}");

    protected Task<JsonNode?> Version(string path, int version) => Get($"{_mount}/data/{path}?version={version}");

    protected Task<EncryptionKeyMigrationReport> Migrate(bool dryRun = false, bool remove = false, CancellationToken cancellationToken = default) =>
        _migrator.Migrate(dryRun, remove, cancellationToken);

    protected async Task<bool> HasPayload(string path, Dictionary<string, string> expected)
    {
        var actual = await _store.Read(path);
        return actual is not null && expected.Count == actual.Data.Count && expected.All(pair => actual.Data.TryGetValue(pair.Key, out var value) && value == pair.Value);
    }

    protected async Task<bool> VersionHasPayload(string path, int version, Dictionary<string, string> expected)
    {
        var data = (await Version(path, version))?["data"];
        return data is JsonObject fields && fields.Count == expected.Count && expected.All(pair => fields[pair.Key]?.GetValue<string>() == pair.Value);
    }

    protected async Task Post(string path, object data)
    {
        using var response = await _http.PostAsJsonAsync($"/v1/{path}", data);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<JsonNode?> Get(string path)
    {
        using var response = await _http.GetAsync($"/v1/{path}");
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())?["data"]?.DeepClone();
    }

    protected async Task<string> Snapshot()
    {
        var snapshot = new SortedDictionary<string, string>(StringComparer.Ordinal);
        await Walk(string.Empty);
        return System.Text.Json.JsonSerializer.Serialize(snapshot);

        async Task Walk(string directory)
        {
            foreach (var leaf in await _store.List(directory))
            {
                var path = $"{directory}{leaf}";
                if (leaf.EndsWith('/'))
                {
                    await Walk(path);
                }
                else
                {
                    await CaptureLeaf(path);
                }
            }
        }

        async Task CaptureLeaf(string path)
        {
            var metadata = await Metadata(path);
            snapshot[$"metadata/{path}"] = metadata?.ToJsonString() ?? "missing";
            foreach (var version in metadata!["versions"]!.AsObject())
            {
                snapshot[$"data/{path}/{version.Key}"] = (await Get($"{_mount}/data/{path}?version={version.Key}"))?.ToJsonString() ?? "missing";
            }
        }
    }
}
