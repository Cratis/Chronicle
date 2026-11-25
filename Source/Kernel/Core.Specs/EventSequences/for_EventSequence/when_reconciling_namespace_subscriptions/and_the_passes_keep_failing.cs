// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Supervision;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_namespace_subscriptions;

public class and_the_passes_keep_failing : given.an_event_sequence
{
    TimeSpan? _first;
    TimeSpan? _second;
    TimeSpan? _afterManyRetries;

    async Task Establish()
    {
        var projections = Substitute.For<IProjectionsManager>();
        var captures = Substitute.For<ICapturesManager>();
        projections.EnsureSubscribedIn(EventStoreNamespace).Returns(_ => Task.FromException(new TimeoutException()));
        _silo.AddProbe(_ => projections);
        _silo.AddProbe(_ => captures);
        await _silo.TimerRegistry.FireAllAsync();
    }

    async Task Because()
    {
        var retry = (Func<Task<TimeSpan?>>)Registrations()[^1].GetArguments()[2]!;
        _first = await retry();
        _second = await retry();
        for (var attempt = 0; attempt < 10; attempt++) _afterManyRetries = await retry();
    }

    global::NSubstitute.Core.ICall[] Registrations() => [.. _supervisor.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(ISiloSupervisor.Register))];

    [Fact] void should_double_the_wait_for_the_first_retry() => _first.ShouldEqual(TimeSpan.FromSeconds(4));
    [Fact] void should_keep_backing_off() => _second.ShouldEqual(TimeSpan.FromSeconds(8));
    [Fact] void should_never_wait_longer_than_the_watchdog_interval() => _afterManyRetries.ShouldEqual(TimeSpan.FromSeconds(new Configuration.Observers().WatchdogInterval));
}
