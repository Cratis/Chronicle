// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

/// <summary>
/// A testable subclass of <see cref="ReplayObserver"/> that exposes step preparation.
/// </summary>
/// <param name="replayStateServiceClient">The <see cref="IObserverServiceClient"/> for replay notifications.</param>
/// <param name="storage">The <see cref="IStorage"/> for accessing key indexes.</param>
/// <param name="jsonSerializerOptions">The serializer options used for JSON serialization.</param>
/// <param name="logger">The logger.</param>
/// <param name="options">The replay configuration.</param>
public class TestableReplayObserver(
    IObserverServiceClient replayStateServiceClient,
    IStorage storage,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<ReplayObserver> logger,
    IOptions<ChronicleOptions>? options = null)
    : ReplayObserver(replayStateServiceClient, storage, jsonSerializerOptions, logger, options), IGrainType
{
    /// <inheritdoc/>
    public Type GrainType => typeof(IReplayObserver);

    /// <summary>
    /// Invokes <see cref="ReplayObserver.PrepareSteps"/> for testing.
    /// </summary>
    /// <param name="request">The request to prepare steps for.</param>
    /// <returns>The prepared steps.</returns>
    public Task<IImmutableList<JobStepDetails>> PrepareStepsForTesting(ReplayObserverRequest request) =>
        PrepareSteps(request);

    /// <summary>
    /// Records a step result for testing the replay's per-partition coverage.
    /// </summary>
    /// <param name="stepId">The step that completed.</param>
    /// <param name="result">Its result.</param>
    /// <returns>Awaitable task.</returns>
    public Task RecordStepForTesting(JobStepId stepId, JobStepResult result) => OnStepCompletedOrStopped(stepId, result);

    /// <summary>
    /// Gets the number of steps the job keeps active at once.
    /// </summary>
    public int? MaxActiveStepsForTesting => MaxActiveSteps;

    /// <summary>
    /// Asks the job whether it runs a step that failed an attempt again.
    /// </summary>
    /// <param name="stepId">The step that failed.</param>
    /// <param name="result">The result of the failed attempt.</param>
    /// <param name="attempt">The number of the attempt that failed.</param>
    /// <returns>True if the step is run again.</returns>
    public bool ShouldRetryStepForTesting(JobStepId stepId, JobStepResult result, int attempt) => ShouldRetryStep(stepId, result, attempt);

    /// <summary>
    /// Invokes replay completion after preparing its test state.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task CompleteForTesting() => OnAllStepsCompleted();

    /// <summary>
    /// Invokes the work done before resuming the job's steps.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public Task ResumeForTesting() => OnBeforeResumingJobSteps();

    /// <summary>
    /// Invokes the check for whether the job can be resumed.
    /// </summary>
    /// <returns>True if the job can be resumed.</returns>
    public Task<bool> CanResumeForTesting() => CanResume();
}
