// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.for_Projections.when_using_registered_source_metadata;

public class and_historical_ownership_is_inherited_from_the_current_generation : an_explicit_store
{
    EventStore _legacyStore;
    EventStore _explicitStore;
    string _legacySequence;
    string _explicitSequence;
    string _wireOwner;

    void Establish()
    {
        _options.EventGenerationDelivery = EventGenerationDelivery.Pinned;
        _artifacts.EventTypes.Returns([typeof(Previous), typeof(Current)]);
        _artifacts.ModelBoundProjections.Returns([typeof(Model)]);
        _legacyStore = CreateStore();
        _options.ExplicitArtifacts.RegisterEventType<Previous>().RegisterEventType<Current>();
        _explicitStore = CreateStore();
    }

    async Task Because()
    {
        await _legacyStore.DiscoverAll();
        await _explicitStore.DiscoverAll();
        _legacySequence = ((Projections)_legacyStore.Projections).Definitions.Single().EventSequenceId;
        _explicitSequence = ((Projections)_explicitStore.Projections).Definitions.Single().EventSequenceId;
        await _explicitStore.EventTypes.Register();
        _wireOwner = _payload.Types.Single().EventStore;
    }

    [Fact] void should_route_a_legacy_historical_projection_to_the_current_generations_inbox() => _legacySequence.ShouldEqual("inbox-foreign");
    [Fact] void should_route_an_explicitly_nominated_historical_projection_to_the_same_inbox() => _explicitSequence.ShouldEqual(_legacySequence);
    [Fact] void should_publish_the_same_foreign_owner_on_the_wire() => _wireOwner.ShouldEqual("foreign");
    [Fact] void should_collect_the_legacy_foreign_external_subscription() => _legacyStore.Projections.GetExternalEventStoreSubscriptions().Single().EventStoreName.ShouldEqual("foreign");
    [Fact] void should_collect_the_nominated_foreign_external_subscription() => _explicitStore.Projections.GetExternalEventStoreSubscriptions().Single().EventStoreName.ShouldEqual("foreign");
    [Fact] void should_subscribe_to_the_historical_events_resolved_identity() => _explicitStore.Projections.GetExternalEventStoreSubscriptions().Single().EventTypeIds.ShouldContainOnly(new EventTypeId("history-event"));
    [Fact] void should_use_one_inherited_descriptor_for_all_consumers() => _explicitStore.EventTypes.GetDescriptorFor(typeof(Previous), _explicitStore.Name).EventStore.ShouldEqual(_wireOwner);
    [Fact] void should_leave_the_public_declaration_helper_unchanged() => typeof(Previous).GetEventStoreName().ShouldBeNull();

    [EventTypeGenerationFor<Current>(1)]
    public record Previous(string Name);
    [EventType("history-event", generation: 2), EventStore("foreign")]
    public record Current(string Name);
    [FromEvent<Previous>]
    public record Model(string Name);
}
