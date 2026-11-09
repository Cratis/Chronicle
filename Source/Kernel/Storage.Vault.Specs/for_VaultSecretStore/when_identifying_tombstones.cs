// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using VaultSharp;
using VaultSharp.V1.Commons;

namespace Cratis.Chronicle.Storage.Vault.for_VaultSecretStore;

public class when_identifying_tombstones : Specification
{
    readonly Dictionary<string, bool> _results = [];

    async Task Because()
    {
        var client = Substitute.For<IVaultClient>();
        var store = new VaultSecretStore(client, "keys");
        foreach (var (name, json) in new Dictionary<string, string>
        {
            ["deleted"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"2020-01-01T00:00:00Z","destroyed":false}}}}""",
            ["destroyed"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"","destroyed":true}}}}""",
            ["mixed_deleted"] = """{"data":{"current_version":2,"versions":{"1":{"destroyed":true},"2":{"deletion_time":"2020-01-01T00:00:00Z"}}}}""",
            ["live"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"","destroyed":false}}}}""",
            ["earlier_live"] = """{"data":{"current_version":2,"versions":{"1":{"deletion_time":"","destroyed":false},"2":{"destroyed":true}}}}""",
            ["newer_live"] = """{"data":{"current_version":2,"versions":{"1":{"destroyed":true},"2":{"deletion_time":"","destroyed":false}}}}""",
            ["scheduled"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"2999-01-01T00:00:00Z","destroyed":false}}}}""",
            ["missing_current"] = """{"data":{"current_version":2,"versions":{"1":{"destroyed":true}}}}""",
            ["empty"] = """{"data":{"current_version":1,"versions":{}}}""",
            ["unknown"] = """{"data":{}}"""
        })
        {
            client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(name, mountPoint: "keys")
                .Returns(JsonSerializer.Deserialize<Secret<FullSecretMetadata>>(json));
            _results[name] = await store.IsTombstone(name);
        }

        client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync("unreadable", mountPoint: "keys")
            .Returns(Task.FromException<Secret<FullSecretMetadata>>(new HttpRequestException("metadata denied")));
        _results["unreadable"] = await store.IsTombstone("unreadable");
    }

    [Fact] void should_ignore_deleted_versions() => _results["deleted"].ShouldBeTrue();
    [Fact] void should_ignore_destroyed_versions() => _results["destroyed"].ShouldBeTrue();
    [Fact] void should_ignore_versions_that_are_all_deleted_or_destroyed() => _results["mixed_deleted"].ShouldBeTrue();
    [Fact] void should_keep_live_entries() => _results["live"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_an_earlier_live_version() => _results["earlier_live"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_a_newer_live_version() => _results["newer_live"].ShouldBeFalse();
    [Fact] void should_keep_entries_scheduled_for_future_deletion() => _results["scheduled"].ShouldBeFalse();
    [Fact] void should_keep_entries_without_current_version_metadata() => _results["missing_current"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_empty_version_metadata() => _results["empty"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_unknown_metadata() => _results["unknown"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_unreadable_metadata() => _results["unreadable"].ShouldBeFalse();
}
