// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureContentMapper.when_mapping;

public class with_field_assignments : Specification
{
    CaptureContentMapper _mapper;
    JsonObject _result;

    void Establish() => _mapper = new();

    void Because() => _result = _mapper.Map(
        new AppendDefinition(
            "CustomerChanged",
            new WhenClause(WhenClauseType.Added, []),
            new Dictionary<string, string>
            {
                ["email"] = "$.contact.email",
                ["displayName"] = "name",
                ["kind"] = "\"customer\"",
                ["priority"] = "42",
                ["active"] = "True"
            }),
        new CaptureChange(
            "42",
            CaptureChangeType.Added,
            null,
            new JsonObject
            {
                ["name"] = "First",
                ["contact"] = new JsonObject { ["email"] = "first@example.com" }
            }));

    [Fact] void should_resolve_source_item_path() => _result["email"]!.ToString().ShouldEqual("first@example.com");
    [Fact] void should_resolve_bare_property_path() => _result["displayName"]!.ToString().ShouldEqual("First");
    [Fact] void should_resolve_string_literal() => _result["kind"]!.GetValue<string>().ShouldEqual("customer");
    [Fact] void should_resolve_number_literal() => _result["priority"]!.GetValue<long>().ShouldEqual(42);
    [Fact] void should_resolve_boolean_literal() => _result["active"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_only_have_assigned_fields() => _result.Count.ShouldEqual(5);
}
