// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.for_Projections.when_using_registered_source_metadata;

public class and_historical_ownership_is_declared_on_its_own_generation : an_explicit_store
{
    string _sequence;

    void Establish()
    {
        _options.EventGenerationDelivery = EventGenerationDelivery.Pinned;
        _options.ExplicitArtifacts.RegisterEventType<Previous>(_ => _.ForEventStore("previous-owner"))
            .RegisterEventType<Current>().RegisterReadModel<Model>();
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _sequence = ((Projections)_store.Projections).Definitions.Single().EventSequenceId;
    }

    [Fact] void should_keep_exact_generation_descriptor_ownership_ahead_of_inherited_ownership() => _store.EventTypes.GetDescriptorFor(typeof(Previous), _store.Name).EventStore.ShouldEqual("previous-owner");
    [Fact] void should_preserve_model_bound_source_selection_from_the_latest_generation() => _sequence.ShouldEqual("inbox-foreign");
    [Fact] void should_collect_the_latest_generation_external_subscription() => _store.Projections.GetExternalEventStoreSubscriptions().Single().EventStoreName.ShouldEqual("foreign");

    [EventTypeGenerationFor<Current>(1)]
    public record Previous(string Name);
    [EventType("owned-history-event", generation: 2), EventStore("foreign")]
    public record Current(string Name);
    [FromEvent<Previous>]
    public record Model(string Name);
}
