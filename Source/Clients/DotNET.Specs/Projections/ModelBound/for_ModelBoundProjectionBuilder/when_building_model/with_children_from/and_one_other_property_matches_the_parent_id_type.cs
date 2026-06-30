// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class and_one_other_property_matches_the_parent_id_type : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(OwnedGuidRecorded)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(OwnedGuidLedger));

    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_use_the_single_non_child_property_as_parent_key()
    {
        // The child key is excluded, leaving exactly one Guid-typed candidate (the parent reference).
        var eventType = event_types.GetEventTypeFor(typeof(OwnedGuidRecorded)).ToContract();
        var fromDef = _result.Children[nameof(OwnedGuidLedger.Items)].From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.ParentKey.ShouldEqual(nameof(OwnedGuidRecorded.OwnerId));
    }
}

[EventType]
public record OwnedGuidRecorded(Guid OwnerId, Guid Ticket, decimal Amount);

public record OwnedGuidLine([Key] Guid Ticket, decimal Amount);

public record OwnedGuidLedger(
    Guid Id,
    [ChildrenFrom<OwnedGuidRecorded>(key: nameof(OwnedGuidRecorded.Ticket), identifiedBy: nameof(OwnedGuidLine.Ticket))]
    IEnumerable<OwnedGuidLine> Items);
