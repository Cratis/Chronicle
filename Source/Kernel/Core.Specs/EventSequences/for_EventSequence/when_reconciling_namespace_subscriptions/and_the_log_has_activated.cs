// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Projections;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_namespace_subscriptions;

public class and_the_log_has_activated : given.an_event_sequence
{
    IProjectionsManager _projections;
    ICapturesManager _captures;

    void Establish()
    {
        _projections = Substitute.For<IProjectionsManager>();
        _captures = Substitute.For<ICapturesManager>();
        _silo.AddProbe(_ => _projections);
        _silo.AddProbe(_ => _captures);
    }

    async Task Because()
    {
        await _silo.TimerRegistry.FireAllAsync();
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_reconcile_projections_once() => await _projections.Received(1).EnsureSubscribedIn(EventStoreNamespace);
    [Fact] async Task should_reconcile_captures_once() => await _captures.Received(1).EnsureSubscribedIn(EventStoreNamespace);
    [Fact] void should_leave_no_reconcile_timer_behind() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] void should_register_with_the_supervisor_for_departures_only() => _supervisor.Received(1).Register(
        $"event-log:{EventStore}:{EventStoreNamespace}",
        null,
        null,
        Arg.Is<Func<Task>>(duty => duty != null));
}
