// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureContentMapper.when_mapping;

public class without_field_assignments : Specification
{
    CaptureContentMapper _mapper;
    JsonObject _result;

    void Establish() => _mapper = new();

    void Because() => _result = _mapper.Map(
        new AppendDefinition("CustomerChanged", new WhenClause(WhenClauseType.Added, []), new Dictionary<string, string>()),
        new CaptureChange("42", CaptureChangeType.Added, null, new JsonObject { ["name"] = "First", ["age"] = 42 }));

    [Fact] void should_use_the_entire_item_as_content() => _result.Count.ShouldEqual(2);
    [Fact] void should_carry_the_item_values() => _result["name"]!.ToString().ShouldEqual("First");
}
