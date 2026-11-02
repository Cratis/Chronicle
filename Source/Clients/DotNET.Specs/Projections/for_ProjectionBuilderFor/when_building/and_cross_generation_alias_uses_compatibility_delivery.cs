// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_cross_generation_alias_uses_compatibility_delivery : Events.for_EventTypes.given.a_cross_generation_registry
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
        _inherited = inheritedBuilder.Build();
        var ownedBuilder = NewFluentBuilder(_ownedRegistry);
        ownedBuilder.From<OwnedAlias>();
        _owned = ownedBuilder.Build();
    }

    [Fact] void should_resolve_the_inherited_historical_owner() => _inheritedRegistry.GetDescriptorFor(typeof(InheritedAlias)).EventStore.ShouldEqual(CurrentOwner);
    [Fact] void should_resolve_the_explicit_historical_owner() => _ownedRegistry.GetDescriptorFor(typeof(OwnedAlias)).EventStore.ShouldEqual(PreviousOwner);
    [Fact] void should_exclude_the_inherited_alias_from_compatibility_clauses() => _inherited.From.Count.ShouldEqual(0);
    [Fact] void should_keep_inherited_alias_compatibility_delivery() => _inherited.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_use_the_event_log_for_the_excluded_inherited_alias() => _inherited.EventSequenceId.ShouldEqual("event-log");
    [Fact] void should_exclude_the_explicitly_owned_alias_from_compatibility_clauses() => _owned.From.Count.ShouldEqual(0);
    [Fact] void should_keep_explicitly_owned_alias_compatibility_delivery() => _owned.GenerationDelivery.ShouldEqual(Contracts.Observation.EventGenerationDelivery.Compatibility);
    [Fact] void should_use_the_event_log_for_the_excluded_explicitly_owned_alias() => _owned.EventSequenceId.ShouldEqual("event-log");
}
