// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="Revise"/>.
/// </summary>
internal class ReviseValidator : CommandValidator<Revise>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReviseValidator"/> class.
    /// </summary>
    public ReviseValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
        RuleFor(_ => _.EventType).NotNull().WithMessage("Event type is required.");

        // The rule below guards the identifier through the parent rather than as a nested
        // `RuleFor(_ => _.EventType.Id)`: Arc's proxy generator mirrors a nested member rule into
        // TypeScript verbatim, emitting `c.eventType.Id` - the un-camel-cased member on a possibly
        // undefined object - which does not compile. A `Must` is not mirrored at all, so the rule
        // stays server-side, where it is the authority anyway.
        RuleFor(_ => _.EventType)
            .Must(eventType => !string.IsNullOrEmpty(eventType.Id))
            .When(_ => _.EventType is not null)
            .WithMessage("Event type identifier is required.");
        RuleFor(_ => _.Content).NotNull().WithMessage("Event content is required.");
    }
}
