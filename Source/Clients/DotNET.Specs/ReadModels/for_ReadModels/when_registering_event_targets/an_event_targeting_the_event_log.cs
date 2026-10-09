// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class an_event_targeting_the_event_log : given.an_event_target<an_event_targeting_the_event_log.OrderShipped>
{
    [EventType, Public, PublishTo(EventSequences.EventSequenceId.LogId)]
    public record OrderShipped(string Id);

    async Task Because() => await Exercise();

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<EventLogIsNotAPublicationTarget>();
    [Fact] void should_not_register_anything() => _request.ShouldBeNull();
}
