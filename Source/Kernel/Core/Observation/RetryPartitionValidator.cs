// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the validator for <see cref="RetryPartition"/>.
/// </summary>
internal class RetryPartitionValidator : CommandValidator<RetryPartition>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryPartitionValidator"/> class.
    /// </summary>
    public RetryPartitionValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.ObserverId).NotEmpty().WithMessage("Observer identifier is required.");
        RuleFor(_ => _.Partition).NotEmpty().WithMessage("Partition identifier is required.");
    }
}
