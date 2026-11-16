// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_finding_keys_holding_a_child;

public abstract class and_the_collection_is_nested<THarness> : given.a_sink_with_parents_holding_children<THarness>
    where THarness : ISinkHarness, new()
{
    IEnumerable<string> _first;

    protected override string ChildrenPath => "details.children";

    protected override string SchemaJson => """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string" },
            "lastUpdated": { "type": "string" },
            "details": {
              "type": "object",
              "properties": {
                "children": { "type": "array", "items": { "type": "object", "properties": { "childId": { "type": "string" } } } }
              }
            }
          }
        }
        """;

    protected override IEnumerable<IDictionary<string, object?>> ChildrenOf(IDictionary<string, object?> parent) =>
        ((IEnumerable<object>)((IDictionary<string, object?>)parent["details"]!)["children"]!).Cast<IDictionary<string, object?>>();

    protected override IEnumerable<PropertyDifference> ChildDifferences(List<ExpandoObject> children)
    {
        var details = new ExpandoObject();
        ((IDictionary<string, object?>)details)["children"] = children;
        return [new PropertyDifference(new PropertyPath("details"), null, details)];
    }

    async Task Establish() => await SeedParents();

    async Task Because()
    {
        _keys = await _sink.FindKeysHoldingChild(Removal());
        _first = await ChildIdsOf("first");
    }

    [Fact] public void should_find_every_parent_holding_the_child() => KeyValues().ShouldEqual<IEnumerable<string>>(["first", "second"]);
    [Fact] public void should_not_remove_the_child() => _first.ShouldEqual<IEnumerable<string>>([RemovedChild, "kept-on-first"]);
}
