// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class a_private_event_to_a_custom_sequence : given.an_event_target<a_private_event_to_a_custom_sequence.OrderPacked>
{
    [EventType, PublishTo("partner-feed")]
    public record OrderPacked(string Id);

    async Task Because() => await Exercise();

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_publish_to_the_custom_sequence() => _request.ReadModels[0].Sink.EventSequence.EventSequence.ShouldEqual("partner-feed");
    [Fact] void should_not_declare_it_public() => _request.ReadModels[0].Sink.EventSequence.IsPublic.ShouldBeFalse();
}
