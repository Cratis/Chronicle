// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the validator for <see cref="StopJob"/>.
/// </summary>
internal class StopJobValidator : CommandValidator<StopJob>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StopJobValidator"/> class.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to check for the job's existence with.</param>
    public StopJobValidator(IGrainFactory grainFactory)
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.JobId).NotEmpty().WithMessage("Job identifier is required.");

        // A job's existence is scoped to its event store and namespace, so this is a command-level check
        // rather than a cross-cutting ConceptValidator<JobId> - a standalone concept validator has no
        // visibility into the sibling EventStore/Namespace.
        RuleFor(_ => _)
            .MustAsync(async (command, _) =>
            {
                var jobs = await grainFactory.GetJobsManager(command.EventStore, command.Namespace).GetAllJobs();
                return jobs.Any(_ => _.Id == command.JobId);
            })
            .WithMessage("Job does not exist.");
    }
}
