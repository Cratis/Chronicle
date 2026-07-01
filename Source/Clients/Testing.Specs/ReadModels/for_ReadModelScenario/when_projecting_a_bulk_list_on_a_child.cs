// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that a whole <see cref="IReadOnlyList{T}"/> carried on a <c>[ChildrenFrom]</c> child — set from
/// a single child-creating event — materializes on the child, mirroring how a top-level bulk list projects.
/// </summary>
public class when_projecting_a_bulk_list_on_a_child : Specification
{
    ReadModelScenario<NotedOrder> _scenario;
    NotedOrderId _orderId;

    void Establish()
    {
        _scenario = new ReadModelScenario<NotedOrder>();
        _orderId = NotedOrderId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_orderId)
            .Events(
                new NotedOrderOpened("ORD-1"),
                new NotedLineAdded("L1", "First line", [new LineNote("alpha", 1), new LineNote("beta", 2)], [new ConceptListItem("red"), new ConceptListItem("green")]),
                new NotedLineAdded("L2", "Second line", [new LineNote("gamma", 1)], [new ConceptListItem("blue")]));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_lines() => _scenario.Instance!.Lines.Count().ShouldEqual(2);
    [Fact] void should_carry_first_line_notes() => _scenario.Instance!.Lines.First().Notes.Count.ShouldEqual(2);
    [Fact] void should_map_first_note_text() => _scenario.Instance!.Lines.First().Notes[0].Text.ShouldEqual("alpha");
    [Fact] void should_map_first_note_order() => _scenario.Instance!.Lines.First().Notes[0].Order.ShouldEqual(1);
    [Fact] void should_carry_second_line_notes() => _scenario.Instance!.Lines.Last().Notes.Count.ShouldEqual(1);
    [Fact] void should_map_second_line_note_text() => _scenario.Instance!.Lines.Last().Notes[0].Text.ShouldEqual("gamma");
    [Fact] void should_carry_first_line_concept_tags() => _scenario.Instance!.Lines.First().Tags.Count.ShouldEqual(2);
    [Fact] void should_round_trip_first_concept_tag() => _scenario.Instance!.Lines.First().Tags[0].Value.ShouldEqual("red");
    [Fact] void should_round_trip_second_concept_tag() => _scenario.Instance!.Lines.First().Tags[1].Value.ShouldEqual("green");
    [Fact] void should_carry_second_line_concept_tag() => _scenario.Instance!.Lines.Last().Tags[0].Value.ShouldEqual("blue");
}
