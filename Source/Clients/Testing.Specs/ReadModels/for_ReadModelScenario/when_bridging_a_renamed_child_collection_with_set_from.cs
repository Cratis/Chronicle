// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Proves the sanctioned fix for a child-collection name mismatch: <c>[SetFrom&lt;E&gt;(nameof(E.Notes))]</c> on
/// the child property bridges the differently named event list, so the collection materializes correctly
/// without renaming the read-model property.
/// </summary>
public class when_bridging_a_renamed_child_collection_with_set_from : Specification
{
    ReadModelScenario<BridgedNotedOrder> _scenario;
    NotedOrderId _orderId;

    void Establish()
    {
        _scenario = new ReadModelScenario<BridgedNotedOrder>();
        _orderId = NotedOrderId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_orderId)
            .Events(
                new NotedOrderOpened("ORD-1"),
                new NotedLineAdded("L1", "First line", [new LineNote("alpha", 1), new LineNote("beta", 2)], []));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_one_line() => _scenario.Instance!.Lines.Count().ShouldEqual(1);
    [Fact] void should_bridge_the_renamed_collection() => _scenario.Instance!.Lines.First().Annotations.Count.ShouldEqual(2);
    [Fact] void should_map_the_first_bridged_item() => _scenario.Instance!.Lines.First().Annotations[0].Text.ShouldEqual("alpha");
}
