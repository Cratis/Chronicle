// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_cross_generation_alias_is_pinned_only_at_build : Events.for_EventTypes.given.a_cross_generation_registry
{
    EventTypes _inheritedRegistry;
    EventTypes _ownedRegistry;
    ProjectionDefinition _inherited;
    ProjectionDefinition _owned;

    async Task Establish()
    {
        _inheritedRegistry = await CreateAliasRegistry(false);
        _ownedRegistry = await CreateAliasRegistry(true);
    }

    void Because()
    {
        var inheritedBuilder = NewFluentBuilder(_inheritedRegistry);
        inheritedBuilder.From<InheritedAlias>();
        _inherited = inheritedBuilder.BuildWithGenerationDelivery(EventGenerationDelivery.Pinned);
        var ownedBuilder = NewFluentBuilder(_ownedRegistry);
        ownedBuilder.From<OwnedAlias>();
        _owned = ownedBuilder.BuildWithGenerationDelivery(EventGenerationDelivery.Pinned);
    }

    [Fact] void should_resolve_the_inherited_historical_owner() => _inheritedRegistry.GetDescriptorFor(typeof(InheritedAlias)).EventStore.ShouldEqual(CurrentOwner);
    [Fact] void should_resolve_the_explicit_historical_owner() => _ownedRegistry.GetDescriptorFor(typeof(OwnedAlias)).EventStore.ShouldEqual(PreviousOwner);
    [Fact] void should_include_one_inherited_alias_clause_at_build_time() => _inherited.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_inherited_alias_id() => _inherited.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_inherited_alias_generation() => _inherited.From.Keys.Single().Generation.ShouldEqual(1u);
    [Fact] void should_keep_the_inherited_alias_non_tombstone() => _inherited.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_pin_inherited_alias_delivery() => _inherited.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Pinned);
    [Fact] void should_not_collect_the_inherited_owner_retroactively() => _inherited.EventSequenceId.ShouldEqual("event-log");
    [Fact] void should_include_one_explicitly_owned_alias_clause_at_build_time() => _owned.From.Count.ShouldEqual(1);
    [Fact] void should_keep_the_explicitly_owned_alias_id() => _owned.From.Keys.Single().Id.ShouldEqual(EventId);
    [Fact] void should_keep_the_explicitly_owned_alias_generation() => _owned.From.Keys.Single().Generation.ShouldEqual(1u);
    [Fact] void should_keep_the_explicitly_owned_alias_non_tombstone() => _owned.From.Keys.Single().Tombstone.ShouldBeFalse();
    [Fact] void should_pin_explicitly_owned_alias_delivery() => _owned.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Pinned);
    [Fact] void should_not_collect_the_explicit_owner_retroactively() => _owned.EventSequenceId.ShouldEqual("event-log");
}
