// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_PendingDeliveries;

public class when_completing_an_unknown_delivery : given.pending_deliveries
{
    bool _accepted;

    void Because() => _accepted = _pending.TryComplete(99, "partition", ObserverSubscriberResult.Ok(12));

    [Fact] void should_reject_the_result() => _accepted.ShouldBeFalse();
    [Fact] void should_leave_the_first_delivery_pending() => _first.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_leave_the_second_delivery_pending() => _second.Task.IsCompleted.ShouldBeFalse();
}
