// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.for_Projections.when_using_registered_source_metadata;

public class and_model_bound_source_ownership_uses_the_latest_generation : an_explicit_store
{
    string _sequence;
    uint _observedGeneration;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PreviousEvent>(_ => _.WithId("shared-event").Generation(1).ForEventStore("previous-owner"))
            .RegisterEventType<CurrentEvent>(_ => _.WithId("shared-event").Generation(2).ForEventStore("current-owner"))
            .RegisterReadModel<Model>();
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        var definition = ((Projections)_store.Projections).Definitions.Single();
        _sequence = definition.EventSequenceId;
        _observedGeneration = definition.From.Keys.Single().Generation;
    }

    [Fact] void should_preserve_model_bound_source_ownership_from_the_latest_generation() => _sequence.ShouldEqual("inbox-current-owner");
    [Fact] void should_still_observe_the_explicitly_declared_event_generation() => _observedGeneration.ShouldEqual(1U);

    public record PreviousEvent(string Name);
    public record CurrentEvent(string Name);
    [FromEvent<PreviousEvent>]
    public record Model(string Name);
}
