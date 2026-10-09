// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class a_public_event_by_event_store : given.an_event_target<a_public_event_by_event_store.OrderShipped>
{
    [EventType, EventStore("test-event-store")]
    public record OrderShipped(string Id);

    async Task Because() => await Exercise();

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_declare_it_public() => _request.ReadModels[0].Sink.EventSequence.IsPublic.ShouldBeTrue();
}
