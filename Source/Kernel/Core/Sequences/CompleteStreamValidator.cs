// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="CompleteStream"/>.
/// </summary>
internal class CompleteStreamValidator : CommandValidator<CompleteStream>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteStreamValidator"/> class.
    /// </summary>
    public CompleteStreamValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");

        // The stream identifiers are not required here: the empty value is the default stream, and
        // completing the default stream is a business outcome the handler reports as
        // `DefaultStreamCannotBeCompleted` - not an input error to reject before it is asked.
    }
}
