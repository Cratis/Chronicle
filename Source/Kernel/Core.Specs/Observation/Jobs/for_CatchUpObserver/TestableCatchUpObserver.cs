// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver;

/// <summary>
/// A testable subclass of <see cref="CatchUpObserver"/> that exposes the grain interface type
/// to satisfy the <c language="csharp">IFoo → Foo</c> convention expected by the testing infrastructure.
/// </summary>
/// <param name="catchupServiceClient">The <see cref="IObserverServiceClient"/> for catch-up notifications.</param>
/// <param name="storage">The <see cref="IStorage"/> for accessing key indexes.</param>
/// <param name="jsonSerializerOptions">The serializer options used for JSON serialization.</param>
/// <param name="logger">The logger.</param>
public class TestableCatchUpObserver(
    IObserverServiceClient catchupServiceClient,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<CatchUpObserver> logger)
    : CatchUpObserver(catchupServiceClient, storage, jsonSerializerOptions, logger), IGrainType
{
    /// <summary>
    /// Gets the steps the job prepared, or <see langword="null"/> when it has not prepared any yet.
    /// </summary>
    public IImmutableList<JobStepDetails>? PreparedSteps { get; private set; }

    /// <inheritdoc/>
    public Type GrainType => typeof(ICatchUpObserver);

    /// <summary>
    /// Invokes catch-up completion after preparing its test state.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task CompleteForTesting() => OnAllStepsCompleted();

    /// <summary>
    /// Invokes the hook that runs before the steps of a resumed job.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task ResumeHookForTesting() => OnBeforeResumingJobSteps();

    /// <summary>
    /// Sets up the persisted state of the job before a spec acts on it.
    /// </summary>
    /// <param name="request">The request of the job.</param>
    /// <param name="status">The status of the job.</param>
    /// <param name="stepPlan">The step plan of the job, or <see langword="null"/> when none was saved.</param>
    /// <param name="isCompleted">Whether every step of the job has concluded.</param>
    public void SetStateForTesting(CatchUpObserverRequest request, JobStatus status, IReadOnlyList<PlannedJobStep>? stepPlan, bool isCompleted = false)
    {
        State.Request = request;
        State.Status = status;
        State.StepPlan = stepPlan;
        State.Progress = isCompleted ? new() { TotalSteps = 1, SuccessfulSteps = 1 } : new() { TotalSteps = 2, SuccessfulSteps = 1 };
    }

    /// <summary>
    /// Invokes the check for whether the job can be resumed.
    /// </summary>
    /// <returns>True if the job can be resumed.</returns>
    public Task<bool> CanResumeForTesting() => CanResume();

    /// <inheritdoc/>
    protected override async Task<IImmutableList<JobStepDetails>> PrepareSteps(CatchUpObserverRequest request)
    {
        PreparedSteps = await base.PrepareSteps(request);
        return PreparedSteps;
    }
}
