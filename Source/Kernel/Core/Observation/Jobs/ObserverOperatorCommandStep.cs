// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents the step that gives an observer the command of an <see cref="ObserverOperatorCommandJob"/>.
/// </summary>
/// <param name="state"><see cref="IPersistentState{TState}"/> for the step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/>.</param>
/// <param name="grainFactory">The thread-safe grain factory for the off-activation job worker.</param>
/// <param name="logger">The logger.</param>
/// <remarks>
/// The call to the observer waits behind whatever the observer is doing, and gives up with a timeout when that takes too
/// long. A timeout says nothing about the command - it is still in the observer's queue, and giving it again changes
/// nothing, because both commands are idempotent - so the step gives it again, a bounded number of times, instead of
/// failing the job.
/// </remarks>
public class ObserverOperatorCommandStep(
    [PersistentState(nameof(ObserverOperatorCommandStepState), Cratis.Choreography.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<ObserverOperatorCommandStepState> state,
    IJobStepThrottle throttle,
    IGrainFactory grainFactory,
    ILogger<ObserverOperatorCommandStep> logger) : JobStep<ObserverOperatorCommandRequest, object, ObserverOperatorCommandStepState>(state, throttle, logger), IObserverOperatorCommandStep
{
    /// <summary>
    /// The most times the command is given to the observer when the observer does not answer in time.
    /// </summary>
    internal const int MaxAttempts = 20;

    /// <summary>
    /// The message of the failure when the observer did not start a replay.
    /// </summary>
    internal const string ReplayNotStartedMessage = "Replay could not be started for this observer.";

    /// <summary>
    /// The time to wait before giving the command to the observer again.
    /// </summary>
    internal static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gives the observer the command.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> to give the command to.</param>
    /// <param name="command">The <see cref="ObserverOperatorCommand"/>.</param>
    /// <param name="maxAttempts">The most times to give it when the observer does not answer in time.</param>
    /// <param name="retryDelay">The time to wait before giving it again.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidOperationException">The observer did not start what the command asked for.</exception>
    /// <exception cref="TimeoutException">The observer did not answer in any attempt.</exception>
    internal static async Task Give(IObserver observer, ObserverOperatorCommand command, int maxAttempts, TimeSpan retryDelay, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await Run(observer, command, attempt == 1);
                return;
            }
            catch (TimeoutException) when (attempt < maxAttempts)
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
        }
    }

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(ObserverOperatorCommandRequest request) =>
        Task.FromResult(Result.Success<PrepareJobStepError>());

    /// <inheritdoc/>
    protected override ValueTask InitializeState(ObserverOperatorCommandRequest request)
    {
        State.EventStore = request.ObserverKey.EventStore;
        State.Namespace = request.ObserverKey.Namespace;
        State.ObserverId = request.ObserverKey.ObserverId;
        State.EventSequenceId = request.ObserverKey.EventSequenceId;
        State.Command = request.Command;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override ValueTask<object?> CreateCancelledResultFromCurrentState(ObserverOperatorCommandStepState currentState) => ValueTask.FromResult<object?>(null);

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(ObserverOperatorCommandStepState currentState, CancellationToken cancellationToken)
    {
        try
        {
            var key = new ObserverKey(
                (ObserverId)currentState.ObserverId,
                (EventStoreName)currentState.EventStore,
                (EventStoreNamespaceName)currentState.Namespace,
                (EventSequenceId)currentState.EventSequenceId);
            await Give(grainFactory.GetGrain<IObserver>(key), currentState.Command, MaxAttempts, RetryDelay, cancellationToken);
            return JobStepResult.Succeeded(null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Stopped, not failed: the job stays resumable.
            return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    static async Task Run(IObserver observer, ObserverOperatorCommand command, bool firstAttempt)
    {
        switch (command)
        {
            case ObserverOperatorCommand.Replay:
                if (await observer.Replay() == JobId.NotSet)
                {
                    throw new InvalidOperationException(ReplayNotStartedMessage);
                }

                break;

            case ObserverOperatorCommand.RetryFailedPartitions:
                var failedPartitions = (await observer.GetFailedPartitionKeys()).ToArray();

                // A repeated attempt may find the partitions the one before it already started, which start nothing again.
                if (await RetryFailedPartitions.Start(observer, failedPartitions) == 0 && firstAttempt && failedPartitions.Length > 0)
                {
                    throw new InvalidOperationException("Recovery could not be started for any failed partition of this observer.");
                }

                break;
        }
    }
}
