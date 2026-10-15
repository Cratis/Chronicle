// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

namespace Cratis.Chronicle.Projections.for_Projections.when_using_registered_source_metadata;

public class and_polymorphic_events_use_effective_foreign_ownership : an_explicit_store
{
    string _sequence;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<RegisteredEvent>(_ => _.WithId("polymorphic-event").ForEventStore("foreign"))
            .RegisterProjection<Model>(_ => _.From<IContract>(), "polymorphic-projection");
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _sequence = ((Projections)_store.Projections).Definitions.Single().EventSequenceId;
    }

    [Fact] void should_infer_the_source_from_registered_concrete_dispatch_events() => _sequence.ShouldEqual("inbox-foreign");
    [Fact] void should_include_the_concrete_event_in_the_external_subscription() => _store.Projections.GetExternalEventStoreSubscriptions().Single().EventTypeIds.ShouldContainOnly(new EventTypeId("polymorphic-event"));

    public interface IContract;
    public record RegisteredEvent(string Name) : IContract;
    public record Model(string Name);
}
