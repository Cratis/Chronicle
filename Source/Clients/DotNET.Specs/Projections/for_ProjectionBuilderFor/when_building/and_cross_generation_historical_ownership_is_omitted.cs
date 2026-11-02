// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_cross_generation_historical_ownership_is_omitted : Events.for_EventTypes.given.a_cross_generation_registry
{
    EventTypes _registry;
    ProjectionDefinition _definition;

    async Task Establish() => _registry = await CreateOwnerlessRegistry();

    void Because()
    {
        var builder = NewFluentBuilder(_registry);
        builder.From<OwnerlessPrevious>();
        _definition = builder.Build();
    }

    [Fact] void should_not_inherit_ownership_just_from_a_shared_id() => _registry.GetDescriptorFor(typeof(OwnerlessPrevious)).EventStore.ShouldBeNull();
    [Fact] void should_still_know_the_current_owner() => _registry.GetDescriptorFor(typeof(AttributeCurrent)).EventStore.ShouldEqual(CurrentOwner);
    [Fact] void should_have_one_previous_clause() => _definition.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_previous_id() => _definition.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_previous_generation() => _definition.From.Keys.Single().Generation.ShouldEqual(1u);
    [Fact] void should_keep_the_previous_non_tombstone() => _definition.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_use_compatibility_delivery() => _definition.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_use_the_event_log_without_a_historical_owner() => _definition.EventSequenceId.ShouldEqual("event-log");
}
