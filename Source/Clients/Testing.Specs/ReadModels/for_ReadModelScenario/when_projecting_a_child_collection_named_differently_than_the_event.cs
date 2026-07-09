// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Documents the silent AutoMap-to-nothing failure: when a child bulk-list property is named differently from
/// the event's list and nothing bridges them, the child materializes with an empty list — no error at runtime.
/// CHR0030 (compile-time) and the projection-factory warning (runtime) exist to surface exactly this.
/// </summary>
public class when_projecting_a_child_collection_named_differently_than_the_event : Specification
{
    ReadModelScenario<MismatchNotedOrder> _scenario;
    NotedOrderId _orderId;

    void Establish()
    {
        _scenario = new ReadModelScenario<MismatchNotedOrder>();
        _orderId = NotedOrderId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_orderId)
            .Events(
                new NotedOrderOpened("ORD-1"),
                new NotedLineAdded("L1", "First line", [new LineNote("alpha", 1), new LineNote("beta", 2)], []));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_still_materialize_the_child() => _scenario.Instance!.Lines.Count().ShouldEqual(1);
    [Fact] void should_leave_the_mismatched_collection_empty() => _scenario.Instance!.Lines.First().Annotations.Count.ShouldEqual(0);
}
