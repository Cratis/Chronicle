// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.for_Projections.when_using_registered_source_metadata;

public class and_a_model_bound_source_uses_its_exact_generation : an_explicit_store
{
    string _sequence;

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
        _sequence = ((Projections)_store.Projections).Definitions.Single().EventSequenceId;
    }

    [Fact] void should_use_the_declared_sources_generation_instead_of_the_latest_generation() => _sequence.ShouldEqual("inbox-previous-owner");

    public record PreviousEvent(string Name);
    public record CurrentEvent(string Name);
    [FromEvent<PreviousEvent>]
    public record Model(string Name);
}
