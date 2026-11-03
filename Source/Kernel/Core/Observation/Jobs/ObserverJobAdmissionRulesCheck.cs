// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a silo lifecycle participant that refuses to start a silo whose observer job admission rules share an order.
/// </summary>
/// <param name="rules">The registered <see cref="IObserverJobAdmissionRule"/> instances.</param>
/// <remarks>
/// <see cref="ObserverJobAdmission"/> is only created when the first job asks to run. Checking at startup makes a
/// misregistered rule stop the silo rather than fail every observer job later.
/// </remarks>
public class ObserverJobAdmissionRulesCheck(IEnumerable<IObserverJobAdmissionRule> rules) : ILifecycleParticipant<ISiloLifecycle>
{
    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(ObserverJobAdmissionRulesCheck), ServiceLifecycleStage.First, Check);

    /// <summary>
    /// Checks that no two rules share an order.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the lifecycle stage.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="DuplicateObserverJobAdmissionRuleOrder">Thrown when two rules share an order.</exception>
    internal Task Check(CancellationToken cancellationToken)
    {
        ObserverJobAdmission.Ordered(rules);
        return Task.CompletedTask;
    }
}
