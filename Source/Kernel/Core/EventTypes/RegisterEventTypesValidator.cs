// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the validator for <see cref="RegisterEventTypes"/>.
/// </summary>
internal class RegisterEventTypesValidator : CommandValidator<RegisterEventTypes>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterEventTypesValidator"/> class.
    /// </summary>
    public RegisterEventTypesValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Types).NotNull().WithMessage("Event type registrations are required.");
        RuleForEach(_ => _.Types).ChildRules(registration =>
        {
            registration.RuleFor(_ => _.Type).NotNull().WithMessage("Event type is required.");
            registration.RuleFor(_ => _.Type.Id).NotEmpty().When(_ => _.Type is not null).WithMessage("Event type identifier is required.");
        });
    }
}
