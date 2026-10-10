// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for scoped stream completion.
/// </summary>
internal class CompleteStreamScopeValidator : CommandValidator<CompleteStreamScope>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteStreamScopeValidator"/> class.
    /// </summary>
    public CompleteStreamScopeValidator()
    {
        RuleFor(command => command.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(command => command.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(command => command.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
    }
}
