// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents a validator for <see cref="SetInitialAdminPassword"/>.
/// </summary>
internal class SetInitialAdminPasswordValidator : CommandValidator<SetInitialAdminPassword>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetInitialAdminPasswordValidator"/> class.
    /// </summary>
    public SetInitialAdminPasswordValidator()
    {
        RuleFor(_ => _.UserId)
            .NotEmpty()
            .WithMessage("User identifier is required.");

        RuleFor(_ => _.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.");

        RuleFor(_ => _.ConfirmedPassword)
            .NotEmpty()
            .WithMessage("Password confirmation is required.")
            .Equal(_ => _.Password)
            .WithMessage("Password confirmation must match the password.");
    }
}
