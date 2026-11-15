// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using VaultSharp;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck;

public class when_reading_mount_metadata : Specification
{
    readonly Dictionary<string, bool> _results = [];

    async Task Because()
    {
        foreach (var (name, json) in new Dictionary<string, string>
        {
            ["v2"] = """{"data":{"type":"kv","options":{"version":"2"}}}""",
            ["v1"] = """{"data":{"type":"kv","options":{"version":"1"}}}""",
            ["other"] = """{"data":{"type":"transit","options":{"version":"2"}}}""",
            ["missing"] = """{"data":{"type":"kv"}}""",
            ["numeric"] = """{"data":{"type":"kv","options":{"version":2}}}"""
        })
        {
            var client = Substitute.For<IVaultClient>();
            client.V1.System.GetSecretBackendAsync("secret").Returns(JsonSerializer.Deserialize<Secret<SecretsEngine>>(json));
            _results[name] = await new VaultSecretStore(client).IsKv2();
        }
    }

    [Fact] void should_accept_kv_v2() => _results["v2"].ShouldBeTrue();
    [Fact] void should_refuse_kv_v1() => _results["v1"].ShouldBeFalse();
    [Fact] void should_refuse_other_engines() => _results["other"].ShouldBeFalse();
    [Fact] void should_refuse_unproven_versions() => _results["missing"].ShouldBeFalse();
    [Fact] void should_refuse_malformed_versions() => _results["numeric"].ShouldBeFalse();
}
