// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_getting_event_types_for;

/// <summary>
/// The event types drive what the reactor subscribes to. An event type handled only during replay still has to
/// be subscribed to, or the replay it exists for would never deliver it.
/// </summary>
public class and_only_a_replay_handler_handles_a_type : Specification
{
    IImmutableList<EventType> _result;

    void Because() => _result = ReactorInvoker.GetEventTypesFor(
        new EventTypesForSpecifications([typeof(MyEvent)]),
        typeof(ReactorWithOnlyAReplayHandler));

    [Fact] void should_subscribe_to_the_event_type() => _result.Count.ShouldEqual(1);

    class ReactorWithOnlyAReplayHandler : IReactor
    {
        [Replay]
        public void HandleDuringReplay(MyEvent @event)
        {
        }
    }
}
