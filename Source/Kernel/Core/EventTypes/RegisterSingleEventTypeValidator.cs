// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the validator for <see cref="RegisterSingleEventType"/>.
/// </summary>
internal class RegisterSingleEventTypeValidator : CommandValidator<RegisterSingleEventType>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterSingleEventTypeValidator"/> class.
    /// </summary>
    public RegisterSingleEventTypeValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Type).NotNull().WithMessage("Event type registration is required.");

        // The rule below guards the identifier through the parent rather than as a nested
        // `RuleFor(_ => _.EventType.Id)`: Arc's proxy generator mirrors a nested member rule into
        // TypeScript verbatim, emitting `c.eventType.Id` - the un-camel-cased member on a possibly
        // undefined object - which does not compile. A `Must` is not mirrored at all, so the rule
        // stays server-side, where it is the authority anyway.
        RuleFor(_ => _.Type)
            .Must(registration => registration.Type is not null && !string.IsNullOrEmpty(registration.Type.Id))
            .When(_ => _.Type is not null)
            .WithMessage("Event type identifier is required.");
    }
}
