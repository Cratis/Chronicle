// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep.given;

public class an_observer_to_give_commands_to : Specification
{
    protected IObserver _observer;

    void Establish() => _observer = Substitute.For<IObserver>();

    protected Task Give(ObserverOperatorCommand command, int maxAttempts = 3) =>
        ObserverOperatorCommandStep.Give(_observer, command, maxAttempts, TimeSpan.Zero, CancellationToken.None);

    protected Task Give(ObserverOperatorCommand command, string partition, bool retryImmediately = false) =>
        ObserverOperatorCommandStep.Give(_observer, command, 3, TimeSpan.Zero, CancellationToken.None, partition, retryImmediately);
}
