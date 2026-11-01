// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Validates a request to complete a definition change.
/// </summary>
internal class CompleteDefinitionChangeValidator : CommandValidator<CompleteDefinitionChange>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteDefinitionChangeValidator"/> class.
    /// </summary>
    public CompleteDefinitionChangeValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.ObserverId).NotEmpty().WithMessage("Observer identifier is required.");
        RuleFor(_ => _.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
        RuleFor(_ => _.Reason).NotEmpty().WithMessage("Reason is required.");
    }
}
