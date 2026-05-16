// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Represents the quarantined state of an observer.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="logger">Logger for logging.</param>
public class QuarantinedObserver(
    ObserverKey observerKey,
    ILogger<QuarantinedObserver> logger) : BaseObserverState
{
    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Quarantined;

    /// <inheritdoc/>
    protected override IImmutableList<Type> AllowedTransitions => new[]
    {
        typeof(Routing)
    }.ToImmutableList();

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var scope = logger.BeginScope(new
        {
            state.Identifier,
            observerKey.EventStore,
            observerKey.Namespace,
            observerKey.EventSequenceId
        });
        logger.ObserverQuarantined();

        var observer = (Observer)Observer;
        await observer.RemoveFailedPartitionReminders();
        await observer.StopAllRetryFailedPartitionJobs();

        return state;
    }
}
