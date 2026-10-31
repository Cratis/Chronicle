// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

namespace Cratis.Chronicle.Events.for_ObserverEventTypes;

public class when_current_store_is_missing : an_explicit_store
{
    EventSequenceId _sequence;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<Recorded>(builder => builder.WithId("local"));
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _sequence = ObserverEventTypes.InferSequence(typeof(Observer), _store.EventTypes, null!, _store.EventTypes.All);
    }

    [Fact] void should_preserve_log_inference_for_events_without_foreign_ownership() => _sequence.ShouldEqual(EventSequenceId.Log);

    public record Recorded;
    public class Observer;
}
