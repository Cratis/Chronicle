// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateConverters;

public class when_round_tripping_a_delivery_fence : Specification
{
    Concepts.Observation.FailedPartitionAttempt _result;
    readonly DeliveryFenceContext _context = new(DateTimeOffset.UnixEpoch.AddSeconds(330), [new("type", "key", "A", "silo")]);

    void Because()
    {
        var state = new Observation.ObserverState
        {
            FailedPartitions = [new Concepts.Observation.FailedPartition
            {
                Partition = "partition",
                Attempts = [new Concepts.Observation.FailedPartitionAttempt { Kind = FailureKind.Timeout, DeliveryFence = _context }]
            }]
        };
        _result = state.ToSql().ToKernel().FailedPartitions.Single().LastAttempt;
    }

    [Fact] void should_preserve_the_original_deadline() => _result.DeliveryFence!.ReleaseBy.ShouldEqual(_context.ReleaseBy);
    [Fact] void should_preserve_the_original_identity() => _result.DeliveryFence!.Targets.ShouldContainOnly(_context.Targets);
    [Fact] void should_preserve_the_timeout_kind() => _result.Kind.ShouldEqual(FailureKind.Timeout);
}
