// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_PendingDeliveries;

public class when_a_wait_is_cancelled : given.tracked_deliveries
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _retry = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ulong _id;
    bool _removed;
    bool _acceptedLateAnswer;
    bool _acceptedLegacyAnswer;

    void Establish() => _id = _pending.Add("partition", _completion);

    void Because()
    {
        _completion.SetCanceled();
        _removed = HasEntryCount("_byId", 0) && HasEntryCount("_byPartition", 0);
        _pending.Add("partition", _retry);
        _acceptedLateAnswer = _pending.TryComplete(_id, "partition", ObserverSubscriberResult.Ok(1));
        _acceptedLegacyAnswer = _pending.TryComplete(0, "partition", ObserverSubscriberResult.Ok(2));
    }

    [Fact] void should_remove_the_id_and_partition_entries() => _removed.ShouldBeTrue();
    [Fact] void should_ignore_the_late_answer() => _acceptedLateAnswer.ShouldBeFalse();
    [Fact] void should_match_the_retry_for_a_legacy_client() => _acceptedLegacyAnswer.ShouldBeTrue();
    [Fact] async Task should_complete_the_retry() => (await _retry.Task).LastSuccessfulObservation.Value.ShouldEqual(2UL);
}
