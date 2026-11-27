// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_CompositeKeyCandidate;

public class when_no_string_leaf_is_safe : Specification
{
    string?[] _leaves;

    void Because()
    {
        var cyclic = new Dictionary<string, object?> { ["tenant"] = "target" };
        cyclic["self"] = cyclic;
        _leaves = new object?[]
        {
            new { number = 7L, flag = true, missing = (object?)null },
            new JsonObject(),
            new { values = Array.Empty<object>() },
            JsonNode.Parse("""{"tenant":"target","Tenant":"target"}"""),
            cyclic,
            new UnreadableIdentity()
        }.Select(CompositeKeyCandidate.StringLeaf).ToArray();
    }

    [Fact] void should_retain_the_scan_for_missing_or_unsupported_leaves() => _leaves.ShouldEachConformTo(value => value is null);

    sealed class UnreadableIdentity
    {
        public string Tenant => throw new InvalidOperationException("The identity cannot be read.");
    }
}
