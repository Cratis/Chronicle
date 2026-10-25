// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_with_status;

public class and_an_erased_container : given.a_manager
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
                "profile":{"type":"object","compliance":[{"metadataType":"PII","details":""}],"properties":{"name":{"type":"string"}}},
                "numbers":{"type":"array","items":{"type":"integer"},"compliance":[{"metadataType":"PII","details":""}]}}}
            """);
        _source = JsonNode.Parse("""{"profile":{"name":"protected name"},"numbers":[1,2]}""")!.AsObject();
        await ProtectAndErase();
    }

    async Task Because() => _result = await _manager.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _protected);

    [Fact] void should_report_the_parent_object_boundary() => _result.Statuses["profile"].ShouldEqual(ProtectedValueReleaseStatus.Erased);
    [Fact] void should_report_the_parent_array_boundary() => _result.Statuses["numbers"].ShouldEqual(ProtectedValueReleaseStatus.Erased);
    [Fact] void should_not_invent_child_statuses() => _result.Statuses.Count.ShouldEqual(2);
    [Fact] void should_retain_the_object_ciphertext() => _result.Content["profile"]!.GetValue<string>().ShouldEqual(_protected["profile"]!.GetValue<string>());
    [Fact] void should_retain_the_array_ciphertext() => _result.Content["numbers"]!.GetValue<string>().ShouldEqual(_protected["numbers"]!.GetValue<string>());
}
