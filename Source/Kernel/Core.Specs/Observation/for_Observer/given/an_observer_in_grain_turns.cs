// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// Runs overlap scenarios as single-threaded grain turns. A scenario may start another observer call while the first
/// awaits storage, but their continuations must not execute simultaneously on a concrete Observer as pool threads.
/// </summary>
public class an_observer_in_grain_turns : an_observer
{
    readonly TaskScheduler _turns = new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler;

    /// <summary>
    /// Performs the scenario, preserving its held-operation interleavings on the grain scheduler.
    /// </summary>
    /// <param name="scenario">The overlapping observer actions.</param>
    /// <returns>The completion of the scenario.</returns>
    protected Task InGrainTurn(Func<Task> scenario) => Task.Factory.StartNew(scenario, CancellationToken.None, TaskCreationOptions.None, _turns).Unwrap();
}
