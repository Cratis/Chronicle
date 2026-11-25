// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

public class when_clearing_the_quarantine_of_an_observer : given.an_observer_to_give_commands_to
{
    async Task Because() => await Give(ObserverOperatorCommand.ClearObserverQuarantine);

    [Fact] void should_clear_the_quarantine() => _observer.Received(1).ClearObserverQuarantine();
}
