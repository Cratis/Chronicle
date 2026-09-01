// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_from;

public class and_explicit_key_matches_a_child_property : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([typeof(ScheduledRateAdjusted)]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(RateTimeline));

    [Fact] void should_return_definition() => _result.ShouldNotBeNull();
    [Fact] void should_have_children_definition() => _result.Children.Count.ShouldEqual(1);

    [Fact]
    void should_use_the_matching_child_property_as_identified_by()
    {
        var children = _result.Children[nameof(RateTimeline.Scheduled)];
        children.IdentifiedBy.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(ScheduledRate.EffectiveFrom))));
    }

    [Fact]
    void should_keep_the_explicit_event_key()
    {
        var children = _result.Children[nameof(RateTimeline.Scheduled)];
        var eventType = event_types.GetEventTypeFor(typeof(ScheduledRateAdjusted)).ToContract();
        var from = children.From.Single(candidate => candidate.Key.IsEqual(eventType)).Value;
        from.Key.ShouldEqual(naming_policy.GetPropertyName(new Properties.PropertyPath(nameof(ScheduledRateAdjusted.EffectiveFrom))));
    }

    [Fact]
    void should_keep_event_source_id_as_the_parent_key()
    {
        var children = _result.Children[nameof(RateTimeline.Scheduled)];
        var eventType = event_types.GetEventTypeFor(typeof(ScheduledRateAdjusted)).ToContract();
        var from = children.From.Single(candidate => candidate.Key.IsEqual(eventType)).Value;
        from.ParentKey.ShouldEqual(WellKnownExpressions.EventSourceId);
    }
}

[EventType]
public record ScheduledRateAdjusted(DateOnly EffectiveFrom, decimal HourlyRate);

public record ScheduledRate(DateOnly EffectiveFrom, decimal HourlyRate);

public record RateTimeline(
    [Key] Guid Id,
    [ChildrenFrom<ScheduledRateAdjusted>(key: nameof(ScheduledRateAdjusted.EffectiveFrom))]
    IEnumerable<ScheduledRate> Scheduled);
