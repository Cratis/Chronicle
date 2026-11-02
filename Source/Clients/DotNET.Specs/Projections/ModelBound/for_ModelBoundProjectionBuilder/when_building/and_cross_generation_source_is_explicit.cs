// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building;

public class and_cross_generation_source_is_explicit : Events.for_EventTypes.given.a_cross_generation_registry
{
    EventTypes _registry;
    RegisterEventTypesRequest _request;
    ProjectionDefinition _inbox;
    ProjectionDefinition _custom;

    async Task Establish()
    {
        _registry = await CreateExplicitRegistry();
        _eventTypesService.When(service => service.RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>()))
            .Do(call => _request = call.Arg<RegisterEventTypesRequest>());
    }

    async Task Because()
    {
        await _registry.Register();
        _inbox = new ModelBoundProjectionBuilder(new DefaultNamingPolicy(), _registry, Consumer).Build(typeof(InboxModel));
        _custom = new ModelBoundProjectionBuilder(new DefaultNamingPolicy(), _registry, Consumer).Build(typeof(CustomModel));
    }

    [Fact] void should_have_one_inbox_clause() => _inbox.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_inbox_clause_id() => _inbox.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_inbox_clause_previous_generation() => _inbox.From.Keys.Single().Generation.ShouldEqual(1u);
    [Fact] void should_keep_the_inbox_clause_non_tombstone() => _inbox.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_keep_inbox_compatibility_delivery() => _inbox.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_use_the_explicit_inbox_instead_of_the_latest_owner() => _inbox.EventSequenceId.ShouldEqual(OverrideInbox);
    [Fact] void should_have_one_custom_sequence_clause() => _custom.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_custom_sequence_clause_id() => _custom.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_custom_sequence_clause_current_generation() => _custom.From.Keys.Single().Generation.ShouldEqual(2u);
    [Fact] void should_keep_the_custom_sequence_clause_non_tombstone() => _custom.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_keep_custom_sequence_compatibility_delivery() => _custom.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_use_the_explicit_non_inbox_sequence_instead_of_the_latest_owner() => _custom.EventSequenceId.ShouldEqual(CustomSequence);
    [Fact] void should_keep_one_wire_registration() => _request.Types.Count().ShouldEqual(1);
    [Fact] void should_not_change_the_wire_owner_with_projection_sequence_overrides() => _request.Types.Single().EventStore.ShouldEqual(CurrentOwner);
}
