// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Copy;

/// <summary>
/// Represents a durable job that copies events from one event source to another in the same event sequence.
/// A single step reads the matching source events in order and appends them to the target, keyed so a
/// resumed job never appends a copy twice.
/// </summary>
/// <param name="logger">The <see cref="ILogger{CopyEventSourceEvents}"/> for logging.</param>
public class CopyEventSourceEvents(ILogger<CopyEventSourceEvents> logger) : Job<CopyEventSourceEventsRequest, JobState>, ICopyEventSourceEvents
{
    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(CopyEventSourceEventsRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(
            new[]
            {
                CreateStep<ICopyEventSourceEventsStep>(request)
            }.ToImmutableList());

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"Copy events from {Request.SourceId} to {Request.TargetId}";

    /// <inheritdoc/>
    protected override Task OnAllStepsCompleted()
    {
        if (AllStepsCompletedSuccessfully)
        {
            logger.CopyCompleted(Request.SourceId, Request.TargetId, Request.EventSequenceId);
        }
        else
        {
            logger.CopyFailed(Request.SourceId, Request.TargetId, Request.EventSequenceId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task<bool> CanResume() => Task.FromResult(true);
}
