// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_with_status;

public class and_an_array_element_is_erased : given.a_manager
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
                "contacts":{"type":"array","items":{"type":"object","properties":{
                    "email":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}},
                "profiles":{"type":"array","items":{"type":"object","compliance":[{"metadataType":"PII","details":""}]}}}}
            """);
        _source = JsonNode.Parse("""{"contacts":[{"email":"first@example.com"},{"email":"second@example.com"}],"profiles":[{"name":"protected name"}]}""")!.AsObject();
        await ProtectAndErase();
    }

    async Task Because() => _result = await _manager.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _protected);

    [Fact] void should_use_bracketed_zero_based_indexes_for_member_paths() => _result.Statuses["contacts[0].email"].ShouldEqual(ProtectedValueReleaseStatus.Erased);
    [Fact] void should_preserve_the_next_index() => _result.Statuses["contacts[1].email"].ShouldEqual(ProtectedValueReleaseStatus.Erased);
    [Fact] void should_report_a_whole_element_boundary_without_a_trailing_dot() => _result.Statuses["profiles[0]"].ShouldEqual(ProtectedValueReleaseStatus.Erased);
    [Fact] void should_retain_the_member_ciphertext() => _result.Content["contacts"]![0]!["email"]!.GetValue<string>().ShouldEqual(_protected["contacts"]![0]!["email"]!.GetValue<string>());
    [Fact] void should_retain_the_whole_element_ciphertext() => _result.Content["profiles"]![0]!.GetValue<string>().ShouldEqual(_protected["profiles"]![0]!.GetValue<string>());
    [Fact] void should_report_each_protected_boundary_once() => _result.Statuses.Count.ShouldEqual(3);
}
