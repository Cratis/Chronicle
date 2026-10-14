// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class and_events_have_tags : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result;

    void Establish()
    {
        _events =
        [
            _events[0] with { Context = _events[0].Context with { Tags = ["important", "blue"] } },
            _events[1] with { Context = _events[1].Context with { Tags = ["green"] } },
            _events[2]
        ];
    }

    async Task Because() => _result = await Forward();

    [Fact] void should_forward_all_events() => AppendCalls().ShouldEqual(3);
    [Fact] void should_report_success() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_preserve_each_events_tags() => _inboxSequence.ReceivedCalls()
        .Where(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append))
        .Select(call => call.GetArguments()[9]).Cast<IEnumerable<Tag>>()
        .Zip(_events, (tags, @event) => tags.SequenceEqual(@event.Context.Tags))
        .All(matches => matches).ShouldBeTrue();
}
