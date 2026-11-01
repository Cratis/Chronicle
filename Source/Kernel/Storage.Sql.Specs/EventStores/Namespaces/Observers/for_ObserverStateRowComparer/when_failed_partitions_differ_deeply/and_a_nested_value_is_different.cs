// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateRowComparer.when_failed_partitions_differ_deeply;

public class and_a_nested_value_is_different : Specification
{
    bool _extraAttempt;
    bool _message;
    bool _fenceTarget;
    bool _extraPartition;

    void Because()
    {
        _extraAttempt = Differs(row => row.FailedPartitions = [row.FailedPartitions.Single() with { Attempts = [.. row.FailedPartitions.Single().Attempts, ComparerRows.CreateAttempt()] }]);
        _message = Differs(row => row.FailedPartitions = [row.FailedPartitions.Single() with { Attempts = [row.FailedPartitions.Single().Attempts.Single() with { Messages = ["other"] }] }]);
        _fenceTarget = Differs(row => row.FailedPartitions = [row.FailedPartitions.Single() with { Attempts = [row.FailedPartitions.Single().Attempts.Single() with { DeliveryFence = new DeliveryFenceContext(null, [new DeliveryFenceTarget("type", "key", "connection", "other")]) }] }]);
        _extraPartition = Differs(row => row.FailedPartitions = [.. row.FailedPartitions, ComparerRows.CreateFailedPartition()]);
    }

    [Fact] void should_detect_an_extra_attempt() => _extraAttempt.ShouldBeTrue();
    [Fact] void should_detect_a_changed_message() => _message.ShouldBeTrue();
    [Fact] void should_detect_a_changed_delivery_fence_target() => _fenceTarget.ShouldBeTrue();
    [Fact] void should_detect_an_extra_failed_partition() => _extraPartition.ShouldBeTrue();

    static bool Differs(Action<ObserverState> change)
    {
        var changed = ComparerRows.Create();
        change(changed);
        return !ObserverStateRowComparer.Instance.Equals(ComparerRows.Create(), changed);
    }
}
