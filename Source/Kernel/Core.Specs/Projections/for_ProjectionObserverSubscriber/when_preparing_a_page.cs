// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_preparing_a_page : given.a_page_subscriber
{
    IReadOnlyList<AppendedEvent> _prepared;
    int _handledWhenPrepared = -1;

    void Establish() => _cachedPipeline.PreparePage(Arg.Any<IReadOnlyList<AppendedEvent>>()).Returns(call =>
    {
        _prepared = call.Arg<IReadOnlyList<AppendedEvent>>();
        _handledWhenPrepared = _handled.Count;
        return Task.CompletedTask;
    });

    async Task Because() => await _subscriber.OnNextPage(_events, new(null));

    [Fact] void should_prepare_once() => _cachedPipeline.Received(1).PreparePage(Arg.Any<IReadOnlyList<AppendedEvent>>());
    [Fact] void should_prepare_with_the_whole_page_in_order() => _prepared.Select(_ => _.Context.SequenceNumber).ShouldEqual(_events.Select(_ => _.Context.SequenceNumber));
    [Fact] void should_prepare_before_handling_any_event() => _handledWhenPrepared.ShouldEqual(0);
}
