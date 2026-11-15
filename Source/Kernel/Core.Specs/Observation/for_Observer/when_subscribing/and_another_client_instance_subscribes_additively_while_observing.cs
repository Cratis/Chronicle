// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// An additive registration merges event types, which only the full subscribe does.
/// </summary>
public class and_another_client_instance_subscribes_additively_while_observing : given.an_observer_served_by_one_client_instance
{
    Task Because() => _observer.SubscribeAdditively<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], _secondSilo, _secondClient);

    [Fact] void should_run_the_full_subscribe() => _storageStats.Writes.ShouldBeGreaterThan(0);
}
