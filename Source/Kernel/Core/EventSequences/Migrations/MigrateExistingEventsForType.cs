// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.Migrations;

/// <summary>
/// Represents a job that migrates existing stored events for a specific event type
/// when a new generation has been registered. A single step is created to iterate
/// all events of the type and update their generational content.
/// </summary>
/// <param name="logger">The <see cref="ILogger{MigrateExistingEventsForType}"/> for logging.</param>
public class MigrateExistingEventsForType(
    ILogger<MigrateExistingEventsForType> logger) : Job<MigrateExistingEventsForTypeRequest, JobState>, IMigrateExistingEventsForType
{
    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(MigrateExistingEventsForTypeRequest request) =>
        Task.FromResult<IImmutableList<JobStepDetails>>(
            new[]
            {
                CreateStep<IMigrateExistingEventsForTypeStep>(request)
            }.ToImmutableList());

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"Migrate events for type {Request.EventTypeId}";

    /// <inheritdoc/>
    protected override Task OnAllStepsCompleted()
    {
        if (AllStepsCompletedSuccessfully)
        {
            logger.MigrationCompleted(Request.EventTypeId);
        }
        else
        {
            logger.MigrationFailed(Request.EventTypeId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task<bool> CanResume() => Task.FromResult(true);
}
