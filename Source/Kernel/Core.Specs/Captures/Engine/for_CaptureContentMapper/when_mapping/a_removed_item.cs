// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureContentMapper.when_mapping;

public class a_removed_item : Specification
{
    CaptureContentMapper _mapper;
    JsonObject _result;

    void Establish() => _mapper = new();

    void Because() => _result = _mapper.Map(
        new AppendDefinition(
            "CustomerRemoved",
            new WhenClause(WhenClauseType.Removed, []),
            new Dictionary<string, string> { ["name"] = "$.name" }),
        new CaptureChange("42", CaptureChangeType.Removed, new JsonObject { ["name"] = "First" }, null));

    [Fact] void should_map_from_the_previous_item() => _result["name"]!.ToString().ShouldEqual("First");
}
