// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverPartitionCommands;

public class when_clearing_quarantine : given.an_observer_grain
{
    async Task Because() => await new ClearObserverQuarantine(EventStore, Namespace, ObserverIdentifier, string.Empty).Handle(_grainFactory);

    [Fact] void should_clear_the_quarantine() => _observer.Received(1).ClearObserverQuarantine();
}
