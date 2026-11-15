// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

/// <summary>
/// A retired observer is left alone by the restore; the reconcile guard returns before any fast path.
/// </summary>
public class and_another_instance_of_the_client_is_observing_after_the_observer_was_retired : given.an_observer_restoring_another_client_instance
{
    async Task Establish()
    {
        await _observer.Retire();
        _storageStats.ResetCounts();
    }

    Task Because() => RestoreSecondClient();

    [Fact] async Task should_not_know_the_second_client() => (await _observer.KnowsClient(_secondClient.ConnectionId)).ShouldBeFalse();
    [Fact] void should_not_write_observer_state() => _storageStats.Writes.ShouldEqual(0);
}
