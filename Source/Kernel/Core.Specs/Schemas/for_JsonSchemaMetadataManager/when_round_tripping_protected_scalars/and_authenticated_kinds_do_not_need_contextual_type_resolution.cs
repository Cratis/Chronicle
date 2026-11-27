// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_round_tripping_protected_scalars;

public class and_authenticated_kinds_do_not_need_contextual_type_resolution : given.real_pii_protection
{
    StatusedRelease _statused;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
              "dynamic":{"$dynamicRef":"https://unavailable.example/personal","compliance":[{"metadataType":"PII","details":""}]},
              "recursive":{"$recursiveRef":"https://unavailable.example/personal","compliance":[{"metadataType":"PII","details":""}]}
            }}
            """);
        _plaintext = new() { ["dynamic"] = "001", ["recursive"] = true };
        await Protect();
    }

    async Task Because()
    {
        _result = await _manager.TryRelease("store", "tenant", _schema, "subject", _protected);
        _statused = await _manager.ReleaseWithStatus("store", "tenant", _schema, "subject", _protected);
    }

    [Fact] void should_preserve_the_dynamic_string() => _result["dynamic"].GetValue<string>().ShouldEqual("001");
    [Fact] void should_preserve_the_recursive_boolean() => _result["recursive"].GetValue<bool>().ShouldBeTrue();
    [Fact] void should_release_both_boundaries_with_status() => JsonNode.DeepEquals(_statused.Content, _plaintext).ShouldBeTrue();
    [Fact] void should_report_decryption_not_erasure() => _statused.Statuses.Values.All(status => status == ProtectedValueReleaseStatus.Decrypted).ShouldBeTrue();
}
