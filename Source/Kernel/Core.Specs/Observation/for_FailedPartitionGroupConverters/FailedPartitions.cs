// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGroupConverters;

static class FailedPartitions
{
    public static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public static Concepts.Observation.FailedPartition With(string observerId, string partition, params (FailureKind Kind, int MinutesAfterStart)[] attempts)
    {
        var failedPartition = new Concepts.Observation.FailedPartition
        {
            ObserverId = observerId,
            Partition = partition
        };

        foreach (var (kind, minutes) in attempts)
        {
            failedPartition.AddAttempt(new FailedPartitionAttempt { Kind = kind, Occurred = Start.AddMinutes(minutes) });
        }

        return failedPartition;
    }
}
