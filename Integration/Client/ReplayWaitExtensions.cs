// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Jobs;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Extension methods for waiting until a replay an operator command accepted has finished.
/// </summary>
static class ReplayWaitExtensions
{
    /// <summary>
    /// Waits until the replay that a replay command accepted has finished.
    /// </summary>
    /// <param name="eventStore">The <see cref="IEventStore"/> the replay was requested on.</param>
    /// <param name="acceptedJobId">The <see cref="JobId"/> the replay command returned.</param>
    /// <param name="timeout">Optional timeout for each of the two waits.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A replay command is accepted as an operator command job that gives the replay to the observer and completes
    /// once the observer has started it. The job therefore completes before the replay does: the observer stays in
    /// the replaying state until the replay it started has finished.
    /// </remarks>
    public static async Task WaitTillReplayed(this IEventStore eventStore, JobId acceptedJobId, TimeSpan? timeout = null)
    {
        await eventStore.Jobs.WaitTillJobCompletesOrIsDeleted(acceptedJobId, timeout);

        var observers = ((IChronicleServicesAccessor)eventStore.Connection).Services.Observers;
        await ReadinessWait.Until(
            () => observers.GetObservers(new() { EventStore = eventStore.Name, Namespace = eventStore.Namespace }),
            all => all.All(observer => observer.RunningState != ObserverRunningState.Replaying),
            timeout ?? TimeSpanFactory.DefaultTimeout());
    }
}
