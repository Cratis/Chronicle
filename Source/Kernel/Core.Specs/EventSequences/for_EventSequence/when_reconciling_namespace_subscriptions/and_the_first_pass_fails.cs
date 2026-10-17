// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Projections;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_namespace_subscriptions;

public class and_the_first_pass_fails : given.an_event_sequence
{
    IProjectionsManager _projections;
    ICapturesManager _captures;

    async Task Establish()
    {
        _projections = Substitute.For<IProjectionsManager>();
        _captures = Substitute.For<ICapturesManager>();
        _projections.EnsureSubscribedIn(EventStoreNamespace).Returns(Task.FromException(new TimeoutException()), Task.CompletedTask);
        _captures.EnsureSubscribedIn(EventStoreNamespace).Returns(Task.FromException(new TimeoutException()), Task.CompletedTask);
        _silo.AddProbe(_ => _projections);
        _silo.AddProbe(_ => _captures);
        await _silo.TimerRegistry.FireAllAsync();
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_retry_projections_on_the_next_tick() => await _projections.Received(2).EnsureSubscribedIn(EventStoreNamespace);
    [Fact] async Task should_retry_captures_on_the_next_tick() => await _captures.Received(2).EnsureSubscribedIn(EventStoreNamespace);
}
