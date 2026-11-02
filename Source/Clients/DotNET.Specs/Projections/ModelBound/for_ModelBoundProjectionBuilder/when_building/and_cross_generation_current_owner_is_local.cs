// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class and_cross_generation_current_owner_is_local : Events.for_EventTypes.given.a_cross_generation_registry
{
    EventTypes _registry;
    RegisterEventTypesRequest _request;
    ProjectionDefinition _previous;
    ProjectionDefinition _current;

    async Task Establish()
    {
        _eventStore.Name.Returns((EventStoreName)CurrentOwner);
        _registry = await CreateExplicitRegistry();
        _eventTypesService.When(service => service.RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>()))
            .Do(call => _request = call.Arg<RegisterEventTypesRequest>());
    }

    async Task Because()
    {
        await _registry.Register();
        _previous = new ModelBoundProjectionBuilder(new DefaultNamingPolicy(), _registry, CurrentOwner).Build(typeof(ExplicitPreviousModel));
        _current = new ModelBoundProjectionBuilder(new DefaultNamingPolicy(), _registry, CurrentOwner).Build(typeof(ExplicitCurrentModel));
    }

    [Fact] void should_have_one_previous_clause() => _previous.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_previous_id() => _previous.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_previous_generation() => _previous.From.Keys.Single().Generation.ShouldEqual(1u);
    [Fact] void should_keep_the_previous_non_tombstone() => _previous.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_keep_previous_compatibility_delivery() => _previous.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_use_the_event_log_for_a_previous_reference_when_the_latest_owner_is_local() => _previous.EventSequenceId.ShouldEqual("event-log");
    [Fact] void should_have_one_current_clause() => _current.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_current_id() => _current.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_current_generation() => _current.From.Keys.Single().Generation.ShouldEqual(2u);
    [Fact] void should_keep_the_current_non_tombstone() => _current.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_keep_current_compatibility_delivery() => _current.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_use_the_event_log_for_a_current_reference_when_its_owner_is_local() => _current.EventSequenceId.ShouldEqual("event-log");
    [Fact] void should_keep_one_wire_registration() => _request.Types.Count().ShouldEqual(1);
    [Fact] void should_keep_the_current_wire_owner() => _request.Types.Single().EventStore.ShouldEqual(CurrentOwner);
    [Fact] void should_keep_the_historical_descriptor_owner() => _registry.GetDescriptorFor(typeof(ExplicitPrevious)).EventStore.ShouldEqual(PreviousOwner);
}
