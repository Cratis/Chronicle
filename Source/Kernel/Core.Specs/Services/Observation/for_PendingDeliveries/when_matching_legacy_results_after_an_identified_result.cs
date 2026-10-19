// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_PendingDeliveries;

public class when_matching_legacy_results_after_an_identified_result : given.pending_deliveries
{
    void Establish() => _pending.TryComplete(_secondId, "partition", ObserverSubscriberResult.Ok(20));
    void Because() => _pending.TryComplete(0, "partition", ObserverSubscriberResult.Ok(12));

    [Fact] async Task should_complete_the_oldest_remaining_delivery() => (await _first.Task.WaitAsync(TimeSpan.FromSeconds(5))).LastSuccessfulObservation.Value.ShouldEqual(12UL);
    [Fact] async Task should_preserve_the_identified_result() => (await _second.Task.WaitAsync(TimeSpan.FromSeconds(5))).LastSuccessfulObservation.Value.ShouldEqual(20UL);
    [Fact] void should_leave_other_partitions_pending() => _other.Task.IsCompleted.ShouldBeFalse();
}
