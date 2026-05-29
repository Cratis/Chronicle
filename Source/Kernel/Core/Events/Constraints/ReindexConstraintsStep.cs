// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a step for reindexing changed constraint indexes.
/// </summary>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> for limiting parallel execution.</param>
/// <param name="logger">The logger.</param>
public class ReindexConstraintsStep(
    [PersistentState(nameof(ReindexConstraintsStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<ReindexConstraintsStepState> state,
    IJobStepThrottle throttle,
    ILogger<ReindexConstraintsStep> logger) : JobStep<ReindexConstraintsRequest, object, ReindexConstraintsStepState>(state, throttle, logger), IReindexConstraintsStep
{
    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(ReindexConstraintsRequest request) =>
        Task.FromResult(Result.Success<PrepareJobStepError>());

    /// <inheritdoc/>
    protected override ValueTask InitializeState(ReindexConstraintsRequest request) => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override ValueTask<object?> CreateCancelledResultFromCurrentState(ReindexConstraintsStepState currentState) =>
        ValueTask.FromResult<object?>(null);

    /// <inheritdoc/>
    protected override Task<Catch<JobStepResult>> PerformStep(ReindexConstraintsStepState currentState, CancellationToken cancellationToken) =>
        Task.FromResult<Catch<JobStepResult>>(JobStepResult.Succeeded(null));
}
