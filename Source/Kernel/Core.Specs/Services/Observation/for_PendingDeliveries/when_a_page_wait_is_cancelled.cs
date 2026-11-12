// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_PendingDeliveries;

public class when_a_page_wait_is_cancelled : given.tracked_deliveries
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _other = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ulong _id;
    bool _retainedOther;
    bool _removed;
    bool _acceptedLateAnswer;

    void Establish() => _id = _pending.AddPage(new Dictionary<EventSourceId, TaskCompletionSource<ObserverSubscriberResult>>
    {
        ["first"] = _completion,
        ["second"] = _other
    });

    void Because()
    {
        _completion.SetCanceled();
        _retainedOther = _pending.TryComplete(_id, "second", ObserverSubscriberResult.Ok(2));
        _removed = WaitForEntryCount("_pages", 0);
        _acceptedLateAnswer = _pending.TryComplete(_id, "first", ObserverSubscriberResult.Ok(1));
    }

    [Fact] void should_keep_the_unfinished_partition() => _retainedOther.ShouldBeTrue();
    [Fact] void should_remove_the_empty_page() => _removed.ShouldBeTrue();
    [Fact] void should_ignore_the_late_answer() => _acceptedLateAnswer.ShouldBeFalse();
}
