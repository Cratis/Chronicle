// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_PendingDeliveries;

public class when_completing_a_delivery_for_the_wrong_partition : given.pending_deliveries
{
    bool _accepted;

    void Because() => _accepted = _pending.TryComplete(_firstId, "other", ObserverSubscriberResult.Ok(12));

    [Fact] void should_reject_the_result() => _accepted.ShouldBeFalse();
    [Fact] void should_leave_the_named_delivery_pending() => _first.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_leave_the_other_partition_pending() => _other.Task.IsCompleted.ShouldBeFalse();
}
