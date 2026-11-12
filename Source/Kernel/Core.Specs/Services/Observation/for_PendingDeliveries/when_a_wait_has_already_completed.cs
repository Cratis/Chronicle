// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_PendingDeliveries;

public class when_a_wait_has_already_completed : given.tracked_deliveries
{
    bool _singleRemoved;
    bool _pageRemoved;

    void Establish() => _completion.SetResult(ObserverSubscriberResult.Ok(1));

    void Because()
    {
        _pending.Add("partition", _completion);
        _pending.AddPage(new Dictionary<EventSourceId, TaskCompletionSource<ObserverSubscriberResult>> { ["partition"] = _completion });
        _singleRemoved = WaitForEntryCount("_byId", 0) && WaitForEntryCount("_byPartition", 0);
        _pageRemoved = WaitForEntryCount("_pages", 0);
    }

    [Fact] void should_not_retain_the_single_delivery() => _singleRemoved.ShouldBeTrue();
    [Fact] void should_not_retain_the_page() => _pageRemoved.ShouldBeTrue();
}
