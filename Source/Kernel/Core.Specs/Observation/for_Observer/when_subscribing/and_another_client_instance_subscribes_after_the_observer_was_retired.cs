// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// A retired observer is revived by an explicit registration, which only the full subscribe does.
/// </summary>
public class and_another_client_instance_subscribes_after_the_observer_was_retired : given.an_observer_served_by_one_client_instance
{
    async Task Establish()
    {
        await _observer.Retire();
        _storageStats.ResetCounts();
    }

    Task Because() => SubscribeSecondClient();

    [Fact] void should_run_the_full_subscribe() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
