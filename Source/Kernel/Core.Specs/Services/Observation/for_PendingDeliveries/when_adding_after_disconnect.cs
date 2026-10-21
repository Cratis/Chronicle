// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_PendingDeliveries;

public class when_adding_after_disconnect : given.pending_deliveries
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _late = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _pending.Disconnect(ObserverSubscriberResult.Disconnected());
    void Because() => _pending.Add("partition", _late);

    [Fact] async Task should_not_leave_the_wait_stranded() => (await _late.Task.WaitAsync(TimeSpan.FromSeconds(5))).State.ShouldEqual(ObserverSubscriberState.Disconnected);
}
