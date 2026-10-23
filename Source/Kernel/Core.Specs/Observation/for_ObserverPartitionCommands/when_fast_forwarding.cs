// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverPartitionCommands;

public class when_fast_forwarding : given.an_observer_grain
{
    async Task Because() => await new FastForward(EventStore, Namespace, ObserverIdentifier, string.Empty).Handle(_grainFactory, _storage);

    [Fact] void should_address_the_observers_inbox() => _grainFactory.Received(1).GetGrain<IObserver>(_key);

    [Fact] void should_invoke_the_operation() => _observer.Received(1).FastForward();
}
