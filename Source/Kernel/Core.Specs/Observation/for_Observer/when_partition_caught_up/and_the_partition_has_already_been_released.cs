// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_caught_up;

public class and_the_partition_has_already_been_released : for_Observer.given.an_observer_with_a_handover_log
{
    EventSequenceNumber _beforeReport;
    EventSequenceNumber _afterReport;

    async Task Establish()
    {
        await _observer.Handle("P", [Append("P", 10UL)]);
        _beforeReport = (await _observer.GetState()).NextEventSequenceNumber;
        _eventSequenceStorage.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.PartitionCaughtUp("P", 10UL, 10UL);
        _afterReport = (await _observer.GetState()).NextEventSequenceNumber;
        await _silo.RunCatchUpContinuation(required: false);
    }

    [Fact] void should_leave_the_floor_unchanged() => _afterReport.ShouldEqual(_beforeReport);
    [Fact] void should_not_read_history_again() => _eventSequenceStorage.ReceivedCalls()
        .Any(call => string.Equals(call.GetMethodInfo().Name, nameof(IEventSequenceStorage.GetRange), StringComparison.Ordinal)).ShouldBeFalse();
}
