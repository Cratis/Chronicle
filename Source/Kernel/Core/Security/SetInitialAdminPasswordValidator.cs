// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the validator for <see cref="SetInitialAdminPassword"/>.
/// </summary>
internal class SetInitialAdminPasswordValidator : CommandValidator<SetInitialAdminPassword>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetInitialAdminPasswordValidator"/> class.
    /// </summary>
    public SetInitialAdminPasswordValidator()
    {
        RuleFor(_ => _.UserId).NotEmpty().WithMessage("User identifier is required.");
        RuleFor(_ => _.Password).NotEmpty().MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
        RuleFor(_ => _.ConfirmedPassword).Equal(_ => _.Password).WithMessage("Confirmed password must match the password.");
    }
}
