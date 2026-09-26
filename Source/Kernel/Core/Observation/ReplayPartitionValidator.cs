// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using FluentValidation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the validator for <see cref="ReplayPartition"/>.
/// </summary>
internal class ReplayPartitionValidator : CommandValidator<ReplayPartition>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayPartitionValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read the observer's definition from.</param>
    public ReplayPartitionValidator(IStorage storage)
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.ObserverId).NotEmpty().WithMessage("Observer identifier is required.");
        RuleFor(_ => _.Partition).NotEmpty().WithMessage("Partition identifier is required.");

        // Replaying a single partition of a kernel-owned observer is the same operation at a smaller scope,
        // so it is refused for the same reason.
        RuleFor(_ => _)
            .MustAsync((command, _) => KernelOwnedObserverRules.MayBeReplayed(storage, command.EventStore, command.ObserverId))
            .WithMessage(KernelOwnedObserverRules.OwnedByKernelMessage);
    }
}
