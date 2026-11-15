// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

/// <summary>
/// A replaying observer is not steadily observing, so the restore runs the full reconcile.
/// </summary>
public class and_another_instance_of_the_client_is_replaying : given.an_observer_restoring_another_client_instance
{
    async Task Establish()
    {
        await _observer.Replay();
        _storageStats.ResetCounts();
    }

    Task Because() => RestoreSecondClient();

    [Fact] void should_run_the_full_reconcile() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
