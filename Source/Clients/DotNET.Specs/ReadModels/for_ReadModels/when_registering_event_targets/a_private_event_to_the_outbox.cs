// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class a_private_event_to_the_outbox : given.an_event_target<a_private_event_to_the_outbox.OrderPacked>
{
    [EventType]
    public record OrderPacked(string Id);

    async Task Because() => await Exercise();

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<PrivateEventTypeCannotBePublishedToOutbox>();
    [Fact] void should_not_register_anything() => _request.ShouldBeNull();
}
