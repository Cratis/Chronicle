// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Supervision;
using Moq;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_namespace_subscriptions;

public class and_a_silo_has_left_the_cluster : given.an_event_sequence
{
    Func<Task> _afterDeparture;

    void Establish() => _afterDeparture = (Func<Task>)_supervisor.ReceivedCalls()
        .First(call => call.GetMethodInfo().Name == nameof(ISiloSupervisor.Register))
        .GetArguments()[3]!;

    async Task Because() => await _afterDeparture();

    [Fact] void should_reconcile_the_event_log_through_its_own_grain() => Mock.Get(_silo.GrainFactory.GetGrain<IEventSequence>(_eventSequenceKey.ToString()))
        .Verify(sequence => sequence.ReconcileSubscriptions(), Times.Once);
}
