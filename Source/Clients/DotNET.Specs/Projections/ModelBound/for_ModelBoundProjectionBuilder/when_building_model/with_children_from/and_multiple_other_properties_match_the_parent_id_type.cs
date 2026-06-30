// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class and_multiple_other_properties_match_the_parent_id_type : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(MultiGuidRecorded)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(MultiGuidLedger));

    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_use_the_first_non_child_property_as_parent_key()
    {
        // The child key is excluded; among the remaining same-typed candidates the inference is ambiguous,
        // so the first by declaration order is used (an explicit parentKey disambiguates).
        var eventType = event_types.GetEventTypeFor(typeof(MultiGuidRecorded)).ToContract();
        var fromDef = _result.Children[nameof(MultiGuidLedger.Items)].From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.ParentKey.ShouldEqual(nameof(MultiGuidRecorded.AccountId));
    }
}

[EventType]
public record MultiGuidRecorded(Guid AccountId, Guid CustomerId, Guid Ticket, decimal Amount);

public record MultiGuidLine([Key] Guid Ticket, decimal Amount);

public record MultiGuidLedger(
    Guid Id,
    [ChildrenFrom<MultiGuidRecorded>(key: nameof(MultiGuidRecorded.Ticket), identifiedBy: nameof(MultiGuidLine.Ticket))]
    IEnumerable<MultiGuidLine> Items);
