// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_PendingDeliveries;

public class when_disconnecting : given.pending_deliveries
{
    void Because() => _pending.Disconnect(ObserverSubscriberResult.Disconnected());

    [Fact] async Task should_fail_the_first_delivery() => (await _first.Task.WaitAsync(TimeSpan.FromSeconds(5))).State.ShouldEqual(ObserverSubscriberState.Disconnected);
    [Fact] async Task should_fail_the_second_delivery() => (await _second.Task.WaitAsync(TimeSpan.FromSeconds(5))).State.ShouldEqual(ObserverSubscriberState.Disconnected);
    [Fact] async Task should_fail_other_partitions() => (await _other.Task.WaitAsync(TimeSpan.FromSeconds(5))).State.ShouldEqual(ObserverSubscriberState.Disconnected);
    [Fact] void should_drop_late_results() => _pending.TryComplete(_firstId, "partition", ObserverSubscriberResult.Ok(12)).ShouldBeFalse();
}
