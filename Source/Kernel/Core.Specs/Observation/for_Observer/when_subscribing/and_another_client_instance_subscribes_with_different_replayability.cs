// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// Replayability is part of the definition, so a differing one is written by the full subscribe.
/// </summary>
public class and_another_client_instance_subscribes_with_different_replayability : given.an_observer_served_by_one_client_instance
{
    Task Because() => SubscribeSecondClient(isReplayable: false);

    [Fact] void should_run_the_full_subscribe() => _storageStats.Writes.ShouldBeGreaterThan(0);
    [Fact] void should_write_the_registered_replayability() => _definitionStorage.State.IsReplayable.ShouldBeFalse();
}
