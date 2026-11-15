// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_with_status;

public class and_a_nested_property_is_erased : given.a_manager
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"profile":{"type":"object","properties":{
                "name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},
                "label":{"type":"string"}}}}}
            """);
        _source = JsonNode.Parse("""{"profile":{"name":"protected name","label":"public label"}}""")!.AsObject();
        await ProtectAndErase();
    }

    async Task Because() => _result = await _manager.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _protected);

    [Fact] void should_report_the_dotted_property_path() => _result.Statuses["profile.name"].ShouldEqual(ProtectedValueReleaseStatus.Erased);
    [Fact] void should_retain_nested_ciphertext() => _result.Content["profile"]!["name"]!.GetValue<string>().ShouldEqual(_protected["profile"]!["name"]!.GetValue<string>());
    [Fact] void should_leave_unprotected_content_unchanged() => _result.Content["profile"]!["label"]!.GetValue<string>().ShouldEqual("public label");
    [Fact] void should_only_report_protected_boundaries() => _result.Statuses.Count.ShouldEqual(1);
}
