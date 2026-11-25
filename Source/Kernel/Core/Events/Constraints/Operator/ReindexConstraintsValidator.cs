// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using FluentValidation;

namespace Cratis.Chronicle.Events.Constraints.Operator;

/// <summary>
/// Validates an explicit constraint reindex request against the registered definitions.
/// </summary>
internal class ReindexConstraintsValidator : CommandValidator<ReindexConstraints>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReindexConstraintsValidator"/> class.
    /// </summary>
    /// <param name="grainFactory">The factory for registered constraints and outstanding jobs.</param>
    public ReindexConstraintsValidator(IGrainFactory grainFactory)
    {
        RuleFor(command => command.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(command => command.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(command => command.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
        RuleFor(command => command.ConstraintNames).NotNull().WithMessage("Constraint names are required; use an empty collection for all applicable constraints.");
        RuleFor(command => command).CustomAsync(async (command, context, _) =>
        {
            if (string.IsNullOrEmpty(command.EventStore?.Value) || string.IsNullOrEmpty(command.Namespace?.Value) || string.IsNullOrEmpty(command.EventSequenceId?.Value) || command.ConstraintNames is null) return;
            var definitions = await grainFactory.GetGrain<IConstraints>(command.EventStore).GetDefinitions();
            var selection = ConstraintReindexSelection.For(command, definitions);
            if (selection.Error is not null)
            {
                context.AddFailure(nameof(command.ConstraintNames), selection.Error);
                return;
            }

            var jobs = await grainFactory.GetJobsManager(command.EventStore, command.Namespace).GetOutstandingJobsOf<IReindexConstraints>();
            if (jobs.Any(job => job.Request is ReindexConstraintsRequest request && request.EventSequenceId == command.EventSequenceId))
            {
                context.AddFailure(nameof(command.EventSequenceId), "A constraint reindex is already outstanding for this event sequence.");
            }
        });
    }
}
