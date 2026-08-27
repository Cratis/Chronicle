// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the validator for <see cref="CreateEventType"/>.
/// </summary>
internal class CreateEventTypeValidator : CommandValidator<CreateEventType>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEventTypeValidator"/> class.
    /// </summary>
    public CreateEventTypeValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");

        // Name is the event type being created, not a reference to one that must already exist.
        RuleFor(_ => _.Name).IgnoreConceptRules().NotEmpty().WithMessage("Event type name is required.");
    }
}
