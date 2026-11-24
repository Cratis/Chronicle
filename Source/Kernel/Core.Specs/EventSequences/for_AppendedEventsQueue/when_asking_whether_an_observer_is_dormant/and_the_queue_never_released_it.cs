// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_asking_whether_an_observer_is_dormant;

/// <summary>
/// An instance with a stored cursor and no live subscription is not dormant unless the queue released it: it may have
/// unsubscribed, which only the observer itself can tell.
/// </summary>
public class and_the_queue_never_released_it : given.a_queue_with_an_unsubscribed_instance
{
    bool _dormant;

    async Task Because() => _dormant = await _queue.IsDormant(_observerKey);

    [Fact] void should_not_report_it_as_dormant() => _dormant.ShouldBeFalse();
}
