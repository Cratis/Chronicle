// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_quarantined_partition : an_observer_with_subscription
{
    protected const string Partition = "SomePartition";
    protected const int MaxRetryAttempts = 2;
    protected static readonly EventSequenceNumber LastFailedSequenceNumber = 44UL;

    protected override Observers CreateObserversConfig() => new() { MaxRetryAttempts = MaxRetryAttempts };

    void Establish()
    {
        _failedPartitionsState.RegisterAttempt((Key)Partition, 42UL, ["first"], string.Empty);
        _failedPartitionsState.RegisterAttempt((Key)Partition, 43UL, ["second"], string.Empty);
        _failedPartitionsState.RegisterAttempt((Key)Partition, LastFailedSequenceNumber, ["third"], string.Empty);
        _failedPartitionsState.Quarantine((Key)Partition);
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 1 };
        _failedPartitionsStorageStats.ResetCounts();
    }

    protected Concepts.Observation.FailedPartition FailedPartition => _failedPartitionsState.Partitions.First();
}
