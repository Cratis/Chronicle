// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_CompositeKeyCandidate;

public class when_selecting_a_string_leaf : Specification
{
    string?[] _leaves;

    void Because()
    {
        var identity = new { id = Guid.Parse("ac0ef349-d07d-4a70-af32-791a23883df4"), sequence = 7, nested = new object?[] { null, new { tenant = "target" } } };
        _leaves = new object[] { identity, JsonSerializer.SerializeToElement(identity), JsonSerializer.SerializeToNode(identity)!, JsonSerializer.Serialize(identity) }
            .Select(CompositeKeyCandidate.StringLeaf).ToArray();
    }

    [Fact] void should_find_the_nested_string_in_all_materializations() => _leaves.ShouldEqual(new string?[] { "target", "target", "target", "target" });
    [Fact] void should_not_use_a_canonical_guid_string_as_an_exact_text_constraint() => CompositeKeyCandidate.StringLeaf(JsonNode.Parse("""{"id":"AC0EF349-D07D-4A70-AF32-791A23883DF4"}""")).ShouldBeNull();
}
