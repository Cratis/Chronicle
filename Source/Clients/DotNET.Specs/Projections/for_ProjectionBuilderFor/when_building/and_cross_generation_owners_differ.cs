// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_cross_generation_owners_differ : Events.for_EventTypes.given.a_cross_generation_registry
{
    EventTypes _attributeRegistry;
    EventTypes _explicitRegistry;
    ProjectionDefinition _attributePrevious;
    ProjectionDefinition _attributeCurrent;
    ProjectionDefinition _explicitPrevious;
    ProjectionDefinition _explicitCurrent;
    ProjectionDefinition _localPrevious;
    ProjectionDefinition _localCurrent;

    async Task Establish()
    {
        _attributeRegistry = await CreateAttributeRegistry();
        _explicitRegistry = await CreateExplicitRegistry();
    }

    void Because()
    {
        _attributePrevious = Build<AttributePrevious>(_attributeRegistry);
        _attributeCurrent = Build<AttributeCurrent>(_attributeRegistry);
        _explicitPrevious = Build<ExplicitPrevious>(_explicitRegistry);
        _explicitCurrent = Build<ExplicitCurrent>(_explicitRegistry);
        _eventStore.Name.Returns((EventStoreName)CurrentOwner);
        _localPrevious = Build<ExplicitPrevious>(_explicitRegistry);
        _localCurrent = Build<ExplicitCurrent>(_explicitRegistry);
    }

    static ProjectionDefinition Build<TEvent>(EventTypes registry)
    {
        var builder = NewFluentBuilder(registry);
        builder.From<TEvent>();

        return builder.Build();
    }

    [Fact] void should_have_one_attribute_previous_clause() => _attributePrevious.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_attribute_previous_id() => _attributePrevious.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_attribute_previous_generation() => _attributePrevious.From.Keys.Single().Generation.ShouldEqual(1u);
    [Fact] void should_keep_the_attribute_previous_non_tombstone() => _attributePrevious.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_keep_attribute_previous_compatibility_delivery() => _attributePrevious.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_use_the_attribute_previous_owner_inbox() => _attributePrevious.EventSequenceId.ShouldEqual("inbox-previous-owner");
    [Fact] void should_have_one_attribute_current_clause() => _attributeCurrent.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_attribute_current_id() => _attributeCurrent.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_attribute_current_generation() => _attributeCurrent.From.Keys.Single().Generation.ShouldEqual(2u);
    [Fact] void should_keep_the_attribute_current_non_tombstone() => _attributeCurrent.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_keep_attribute_current_compatibility_delivery() => _attributeCurrent.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_use_the_attribute_current_owner_inbox() => _attributeCurrent.EventSequenceId.ShouldEqual("inbox-current-owner");
    [Fact] void should_have_one_explicit_previous_clause() => _explicitPrevious.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_explicit_previous_id() => _explicitPrevious.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_explicit_previous_generation() => _explicitPrevious.From.Keys.Single().Generation.ShouldEqual(1u);
    [Fact] void should_keep_the_explicit_previous_non_tombstone() => _explicitPrevious.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_keep_explicit_previous_compatibility_delivery() => _explicitPrevious.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_use_the_explicit_previous_owner_inbox() => _explicitPrevious.EventSequenceId.ShouldEqual("inbox-previous-owner");
    [Fact] void should_have_one_explicit_current_clause() => _explicitCurrent.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_explicit_current_id() => _explicitCurrent.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_explicit_current_generation() => _explicitCurrent.From.Keys.Single().Generation.ShouldEqual(2u);
    [Fact] void should_keep_the_explicit_current_non_tombstone() => _explicitCurrent.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_keep_explicit_current_compatibility_delivery() => _explicitCurrent.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_use_the_explicit_current_owner_inbox() => _explicitCurrent.EventSequenceId.ShouldEqual("inbox-current-owner");
    [Fact] void should_have_one_local_previous_clause() => _localPrevious.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_local_previous_id() => _localPrevious.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_local_previous_generation() => _localPrevious.From.Keys.Single().Generation.ShouldEqual(1u);
    [Fact] void should_keep_the_local_previous_non_tombstone() => _localPrevious.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_keep_local_previous_compatibility_delivery() => _localPrevious.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_still_use_the_previous_inbox_with_a_local_current_owner() => _localPrevious.EventSequenceId.ShouldEqual("inbox-previous-owner");
    [Fact] void should_have_one_local_current_clause() => _localCurrent.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_local_current_id() => _localCurrent.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_local_current_generation() => _localCurrent.From.Keys.Single().Generation.ShouldEqual(2u);
    [Fact] void should_keep_the_local_current_non_tombstone() => _localCurrent.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_keep_local_current_compatibility_delivery() => _localCurrent.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_still_use_the_current_inbox_with_a_local_current_owner() => _localCurrent.EventSequenceId.ShouldEqual("inbox-current-owner");
    [Fact] void should_resolve_the_explicit_previous_owner_from_the_registry() => _explicitRegistry.GetDescriptorFor(typeof(ExplicitPrevious)).EventStore.ShouldEqual(PreviousOwner);
    [Fact] void should_resolve_the_explicit_current_owner_from_the_registry() => _explicitRegistry.GetDescriptorFor(typeof(ExplicitCurrent)).EventStore.ShouldEqual(CurrentOwner);
}
