// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;

namespace Cratis.Chronicle.Storage.Vault.for_VaultSecretStore;

public class when_identifying_tombstones : Specification
{
    readonly Dictionary<string, bool> _results = [];

    static Secret<SecretData> ReadVersion(string path, int? version) => (path, version) switch
    {
        ("deleted", 1) or ("mixed_deleted", 2) or ("deleted_without_timestamp", 1) =>
            throw new VaultApiException(HttpStatusCode.NotFound, "version deleted"),
        ("unreadable_version", _) or ("earlier_unreadable", 1) =>
            throw new VaultApiException(HttpStatusCode.Forbidden, "version denied"),
        ("uncertain_version", _) => null!,
        ("deleted_marker", 1) => JsonSerializer.Deserialize<Secret<SecretData>>(
            """{"data":{"data":null,"metadata":{"version":1,"deletion_time":"2020-01-01T00:00:00Z"}}}""")!,
        _ => new Secret<SecretData>
        {
            Data = new SecretData
            {
                Data = new Dictionary<string, object> { ["key"] = "still readable" },
                Metadata = new CurrentSecretMetadata
                {
                    Version = version ?? 0,
                    DeletionTime = path == "scheduled" ? "2999-01-01T00:00:00Z" : "2020-01-01T00:00:00Z"
                }
            }
        }
    };

    async Task Because()
    {
        var client = Substitute.For<IVaultClient>();
        var store = new VaultSecretStore(client, "keys");
        client.V1.Secrets.KeyValue.V2.ReadSecretAsync(Arg.Any<string>(), Arg.Any<int?>(), mountPoint: "keys")
            .Returns(call => ReadVersion(call.ArgAt<string>(0), call.ArgAt<int?>(1)));
        foreach (var (name, json) in new Dictionary<string, string>
        {
            ["deleted"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"2020-01-01T00:00:00Z","destroyed":false}}}}""",
            ["destroyed"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"","destroyed":true}}}}""",
            ["mixed_deleted"] = """{"data":{"current_version":2,"versions":{"1":{"destroyed":true},"2":{"deletion_time":"2020-01-01T00:00:00Z"}}}}""",
            ["live"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"","destroyed":false}}}}""",
            ["earlier_live"] = """{"data":{"current_version":2,"versions":{"1":{"deletion_time":"","destroyed":false},"2":{"destroyed":true}}}}""",
            ["newer_live"] = """{"data":{"current_version":2,"versions":{"1":{"destroyed":true},"2":{"deletion_time":"","destroyed":false}}}}""",

            // Both dates are future deletions on Vault's clock while it still serves the version.
            ["scheduled_client_ahead"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"2020-01-01T00:00:00Z","destroyed":false}}}}""",
            ["scheduled"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"2999-01-01T00:00:00Z","destroyed":false}}}}""",
            ["unreadable_version"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"2020-01-01T00:00:00Z"}}}}""",
            ["earlier_unreadable"] = """{"data":{"current_version":2,"versions":{"1":{"deletion_time":"2020-01-01T00:00:00Z"},"2":{"destroyed":true}}}}""",
            ["uncertain_version"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"2020-01-01T00:00:00Z"}}}}""",
            ["deleted_without_timestamp"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":""}}}}""",
            ["deleted_marker"] = """{"data":{"current_version":1,"versions":{"1":{"deletion_time":"2999-01-01T00:00:00Z"}}}}""",
            ["earlier_scheduled"] = """{"data":{"current_version":2,"versions":{"1":{"deletion_time":"2020-01-01T00:00:00Z"},"2":{"destroyed":true}}}}""",
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
    [Fact] void should_keep_readable_scheduled_versions_when_the_client_clock_is_ahead() => _results["scheduled_client_ahead"].ShouldBeFalse();
    [Fact] void should_keep_readable_scheduled_versions_when_the_client_clock_is_behind() => _results["scheduled"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_an_earlier_readable_scheduled_version() => _results["earlier_scheduled"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_an_unreadable_version() => _results["unreadable_version"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_an_earlier_unreadable_version() => _results["earlier_unreadable"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_an_uncertain_version() => _results["uncertain_version"].ShouldBeFalse();
    [Fact] void should_ignore_versions_confirmed_deleted_without_a_metadata_timestamp() => _results["deleted_without_timestamp"].ShouldBeTrue();
    [Fact] void should_ignore_versions_with_a_deleted_marker_from_vault() => _results["deleted_marker"].ShouldBeTrue();
    [Fact] void should_keep_entries_without_current_version_metadata() => _results["missing_current"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_empty_version_metadata() => _results["empty"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_unknown_metadata() => _results["unknown"].ShouldBeFalse();
    [Fact] void should_keep_entries_with_unreadable_metadata() => _results["unreadable"].ShouldBeFalse();
}
