// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class a_reducer_target : given.an_event_target<a_reducer_target.OrderStatusChanged>
{
    [EventType, Public]
    public record OrderStatusChanged(string Id);

    protected override bool UseReducer => true;

    async Task Because() => await Exercise();

    [Fact] void should_use_the_event_sequence_sink() => _request.ReadModels[0].Sink.TypeId.ShouldEqual("EventSequence");
    [Fact] void should_be_a_reducer_observer() => _request.ReadModels[0].ObserverType.ShouldEqual(Contracts.ReadModels.ReadModelObserverType.Reducer);
    [Fact] void should_link_the_reducer() => _request.ReadModels[0].ObserverIdentifier.ShouldEqual("a-reducer");
}
