// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverPartitionCommands;

public class when_clearing_partition_quarantine : given.an_observer_grain
{
    ClearPartitionQuarantineResult _expected;
    ClearPartitionQuarantineResult _result;

    void Establish()
    {
        _expected = new(ClearPartitionQuarantineOutcome.Cleared, PartitionRecoveryOutcome.Started);
        _observer.ClearPartitionQuarantine(Partition, true).Returns(_expected);
    }

    async Task Because() => _result = await new ClearPartitionQuarantine(EventStore, Namespace, ObserverIdentifier, string.Empty, Partition, true).Handle(_grainFactory, _storage);

    [Fact] void should_address_the_observers_inbox() => _grainFactory.Received(1).GetGrain<IObserver>(_key);

    [Fact] void should_clear_the_partition_quarantine_with_the_retry_flag() => _observer.Received(1).ClearPartitionQuarantine(Partition, true);
    [Fact] void should_return_the_result_the_grain_reported() => _result.ShouldEqual(_expected);
}
