// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Supervision;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_namespace_subscriptions;

public class and_a_pass_fails_and_the_next_succeeds : given.an_event_sequence
{
    IProjectionsManager _projections;
    ICapturesManager _captures;
    int _registrationsAfterFailure;
    TimeSpan? _intervalAfterFailure;

    async Task Establish()
    {
        _projections = Substitute.For<IProjectionsManager>();
        _captures = Substitute.For<ICapturesManager>();
        _projections.EnsureSubscribedIn(EventStoreNamespace).Returns(Task.FromException(new TimeoutException()), Task.CompletedTask);
        _silo.AddProbe(_ => _projections);
        _silo.AddProbe(_ => _captures);
        await _silo.TimerRegistry.FireAllAsync();
        _registrationsAfterFailure = Registrations().Length;
        _intervalAfterFailure = Registrations()[^1].GetArguments()[1] as TimeSpan?;
    }

    async Task Because() => await _eventSequence.ReconcileSubscriptions();

    global::NSubstitute.Core.ICall[] Registrations() => [.. _supervisor.ReceivedCalls().Where(call => call.GetMethodInfo().Name == nameof(ISiloSupervisor.Register))];

    [Fact] void should_ask_the_supervisor_to_retry_promptly_after_the_failure() => _intervalAfterFailure.ShouldEqual(TimeSpan.FromSeconds(2));
    [Fact] void should_have_registered_the_retry_beside_the_departure_duty() => _registrationsAfterFailure.ShouldEqual(2);
    [Fact] void should_stop_retrying_once_a_pass_succeeds() => Registrations()[^1].GetArguments()[1].ShouldBeNull();
}
