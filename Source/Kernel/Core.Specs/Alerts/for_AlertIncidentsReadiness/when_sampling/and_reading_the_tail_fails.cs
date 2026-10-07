// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsReadiness.when_sampling;

public class and_reading_the_tail_fails : given.a_health_sampler
{
    Exception? _error;

    void Establish() => _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default)
        .GetEventSequence(EventSequenceId.System).GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>())
        .Returns(Task.FromException<EventSequenceNumber>(new InvalidAlertIncidentQuery("Storage unavailable.")));
    async Task Because() => _error = await Catch.Exception(_readiness.Get);

    [Fact] void should_not_claim_empty_history_is_ready() => _error.ShouldBeOfExactType<InvalidAlertIncidentQuery>();
}
