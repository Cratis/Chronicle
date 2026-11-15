// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

/// <summary>
/// Replayability is part of the definition, so a differing one is written by the full reconcile.
/// </summary>
public class and_another_instance_of_the_client_is_observing_with_different_replayability : given.an_observer_restoring_another_client_instance
{
    Task Because() => RestoreSecondClient(isReplayable: false);

    [Fact] void should_run_the_full_reconcile() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
