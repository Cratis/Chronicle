// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class and_only_the_child_key_matches_the_parent_id_type : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(SoleGuidRecorded)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(SoleGuidLedger));

    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_default_parent_key_to_event_source_id()
    {
        // The only Guid-typed event property is the child key itself, which is excluded from
        // parent-key inference — so there is no candidate and it falls back to the event source.
        var eventType = event_types.GetEventTypeFor(typeof(SoleGuidRecorded)).ToContract();
        var fromDef = _result.Children[nameof(SoleGuidLedger.Items)].From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.ParentKey.ShouldEqual(WellKnownExpressions.EventSourceId);
    }
}

[EventType]
public record SoleGuidRecorded(Guid Ticket, decimal Amount);

public record SoleGuidLine([Key] Guid Ticket, decimal Amount);

public record SoleGuidLedger(
    Guid Id,
    [ChildrenFrom<SoleGuidRecorded>(key: nameof(SoleGuidRecorded.Ticket), identifiedBy: nameof(SoleGuidLine.Ticket))]
    IEnumerable<SoleGuidLine> Items);
