// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// A replaying observer is not steadily observing, so the registration cannot be handed over in memory.
/// </summary>
public class and_another_client_instance_subscribes_with_same_definition_while_replaying : given.an_observer_served_by_one_client_instance
{
    async Task Establish()
    {
        await _observer.Replay();
        _storageStats.ResetCounts();
    }

    Task Because() => SubscribeSecondClient();

    [Fact] void should_run_the_full_subscribe() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
