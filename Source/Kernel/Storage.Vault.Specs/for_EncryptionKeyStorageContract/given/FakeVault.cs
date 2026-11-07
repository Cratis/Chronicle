// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using VaultSharp;

namespace Cratis.Chronicle.Storage.Vault.for_EncryptionKeyStorageContract.given;

/// <summary>
/// KV v2 wire fake. Destruction leaves CurrentVersion intact: CAS against a destroyed current version succeeds.
/// The SDK runs against this handler; the injected IVaultClient is a substitute delegating to that SDK.
/// </summary>
public sealed class FakeVault : HttpMessageHandler
{
    readonly Dictionary<string, List<Dictionary<string, object>?>> _paths = new(StringComparer.Ordinal);
    readonly object _lock = new();
    readonly HashSet<(string Path, int Version)> _deleted = [];

    public bool CasRequired { get; set; }

    public bool IgnoreCas { get; set; }
    public bool Enterprise { get; set; }
    public bool RefuseReads { get; set; }
    public bool RefuseLists { get; set; }
    public bool RefuseDestroy { get; set; }
    public int StageConflicts { get; set; }

    public static IVaultClient Client(HttpMessageHandler handler)
    {
        var settings = new VaultClientSettings("http://vault.invalid", null) { MyHttpClientProviderFunc = _ => new HttpClient(handler) };
        var sdk = new VaultClient(settings);
        var client = Substitute.For<IVaultClient>();
        client.Settings.Returns(settings);
        client.V1.Returns(sdk.V1);
        return client;
    }

    public void Seed(string path, Dictionary<string, object> data)
    {
        lock (_lock) _paths[path] = [new(data, StringComparer.Ordinal)];
    }

    public int CurrentVersion(string path)
    {
        lock (_lock) return _paths.TryGetValue(path, out var versions) ? versions.Count : 0;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken));
        using (body)
        {
            lock (_lock) return Dispatch(request, body);
        }
    }

    HttpResponseMessage Dispatch(HttpRequestMessage request, JsonDocument? body)
    {
        var route = request.RequestUri!.AbsolutePath.Split('/', 5);
        if (request.RequestUri.AbsolutePath == "/v1/sys/health") return Reply(new { initialized = true, @sealed = false, enterprise = Enterprise, performance_standby = Enterprise });
        var action = route[3];
        if (action == "config")
        {
            CasRequired = body!.RootElement.GetProperty("cas_required").GetBoolean();
            return new(HttpStatusCode.NoContent);
        }
        var path = route.Length == 5 ? Uri.UnescapeDataString(route[4]).TrimEnd('/') : string.Empty;
        if (action == "metadata" && IsList(request)) return List(path);
        _paths.TryGetValue(path, out var versions);
        return action switch
        {
            "metadata" => Metadata(request.Method, path, versions),
            "destroy" => Destroy(body!, versions),
            "delete" => SoftDelete(path, body, versions),
            "undelete" => Undelete(path, body!, versions),
            "data" => Data(request, path, body, versions),
            _ => Error(HttpStatusCode.BadRequest, "unexpected route")
        };
    }

    static bool IsList(HttpRequestMessage request) => request.Method.Method == "LIST" || request.RequestUri!.Query == "?list=true";

    HttpResponseMessage Data(HttpRequestMessage request, string path, JsonDocument? body, List<Dictionary<string, object>?>? versions)
    {
        if (request.Method == HttpMethod.Delete) return SoftDelete(path, null, versions);
        if (request.Method == HttpMethod.Get) return Read(path, request.RequestUri!.Query, versions);
        if (request.Method == HttpMethod.Put || request.Method == HttpMethod.Post) return Write(path, body!, versions);
        return Error(HttpStatusCode.BadRequest, "unexpected route");
    }

    HttpResponseMessage List(string path)
    {
        if (RefuseLists) return Error(HttpStatusCode.Forbidden, "permission denied");
        var prefix = path.Length == 0 ? string.Empty : path + "/";
        var keys = _paths.Keys.Where(_ => _.StartsWith(prefix, StringComparison.Ordinal)).Select(_ => _[prefix.Length..]).Where(_ => _.Length > 0)
            .Select(_ => _.Contains('/', StringComparison.Ordinal) ? _[..(_.IndexOf('/', StringComparison.Ordinal) + 1)] : _).Distinct(StringComparer.Ordinal).ToArray();
        return keys.Length == 0 ? Error(HttpStatusCode.NotFound, "not found") : Reply(new { data = new { keys } });
    }

    HttpResponseMessage Metadata(HttpMethod method, string path, List<Dictionary<string, object>?>? versions)
    {
        if (method == HttpMethod.Delete)
        {
            _paths.Remove(path);
            return new(HttpStatusCode.NoContent);
        }

        if (method == HttpMethod.Get)
        {
            return versions is null ? Error(HttpStatusCode.NotFound, "not found") : Reply(new
            {
                data = new { current_version = versions.Count, versions = versions.Select((data, index) => (data, index)).ToDictionary(_ => (_.index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), _ => new { destroyed = _.data is null, deletion_time = _deleted.Contains((path, _.index + 1)) ? "2026-01-01T00:00:00Z" : string.Empty }) }
            });
        }

        return Error(HttpStatusCode.BadRequest, "unexpected route");
    }

    HttpResponseMessage Destroy(JsonDocument body, List<Dictionary<string, object>?>? versions)
    {
        if (RefuseDestroy) return Error(HttpStatusCode.Forbidden, "permission denied");
        foreach (var version in body.RootElement.GetProperty("versions").EnumerateArray().Select(_ => _.GetInt32()))
        {
            if (versions is not null && version > 0 && version <= versions.Count) versions[version - 1] = null;
        }

        return new(HttpStatusCode.NoContent);
    }

    HttpResponseMessage SoftDelete(string path, JsonDocument? body, List<Dictionary<string, object>?>? versions)
    {
        foreach (var version in body is null ? [versions?.Count ?? 0] : body.RootElement.GetProperty("versions").EnumerateArray().Select(_ => _.GetInt32()).ToArray())
        {
            if (version > 0 && version <= (versions?.Count ?? 0)) _deleted.Add((path, version));
        }

        return new(HttpStatusCode.NoContent);
    }

    HttpResponseMessage Undelete(string path, JsonDocument body, List<Dictionary<string, object>?>? versions)
    {
        foreach (var version in body.RootElement.GetProperty("versions").EnumerateArray().Select(_ => _.GetInt32()))
        {
            if (version > 0 && version <= (versions?.Count ?? 0)) _deleted.Remove((path, version));
        }

        return new(HttpStatusCode.NoContent);
    }

    HttpResponseMessage Read(string path, string query, List<Dictionary<string, object>?>? versions)
    {
        if (RefuseReads) return Error(HttpStatusCode.Forbidden, "permission denied");
        var requested = query.TrimStart('?').Split('&').FirstOrDefault(_ => _.StartsWith("version=", StringComparison.Ordinal));
        var version = requested is null ? versions?.Count ?? 0 : int.Parse(requested["version=".Length..], System.Globalization.CultureInfo.InvariantCulture);
        if (version == 0) version = versions?.Count ?? 0;
        if (versions is null || version <= 0 || version > versions.Count || versions[version - 1] is null || _deleted.Contains((path, version))) return Error(HttpStatusCode.NotFound, "not found");
        return Reply(new { data = new { data = versions[version - 1], metadata = new { version } } });
    }

    HttpResponseMessage Write(string path, JsonDocument body, List<Dictionary<string, object>?>? versions)
    {
        var root = body.RootElement;
        if (root.GetProperty("data").TryGetProperty("writerNonce", out _) && StageConflicts > 0)
        {
            StageConflicts--;
            return Error(HttpStatusCode.BadRequest, "check-and-set parameter did not match the current version");
        }

        if (CasRequired && ExpectedVersion(root) is null) return Error(HttpStatusCode.BadRequest, "check-and-set parameter required for this call");
        if (!IgnoreCas && ExpectedVersion(root) is { } expected && expected != (versions?.Count ?? 0))
        {
            return Error(HttpStatusCode.BadRequest, "check-and-set parameter did not match the current version");
        }

        var data = root.GetProperty("data").EnumerateObject().ToDictionary(_ => _.Name, _ => (object)_.Value.Clone(), StringComparer.Ordinal);
        if (versions is null) _paths[path] = versions = [];
        versions.Add(data);
        return Reply(new { data = new { version = versions.Count } });
    }

    static int? ExpectedVersion(JsonElement root) => root.TryGetProperty("options", out var options) && options.TryGetProperty("cas", out var cas) ? cas.GetInt32() : null;

    static HttpResponseMessage Reply(object value) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };
    static HttpResponseMessage Error(HttpStatusCode status, string message) => new(status) { Content = new StringContent(JsonSerializer.Serialize(new { errors = new[] { message } }), Encoding.UTF8, "application/json") };
}
