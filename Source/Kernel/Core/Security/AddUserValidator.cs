// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the validator for <see cref="AddUser"/>.
/// </summary>
internal class AddUserValidator : CommandValidator<AddUser>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddUserValidator"/> class.
    /// </summary>
    public AddUserValidator()
    {
        RuleFor(_ => _.UserId).NotEmpty().WithMessage("User identifier is required.");
        RuleFor(_ => _.Username).NotEmpty().WithMessage("Username is required.");
        RuleFor(_ => _.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(_ => _.Password).NotEmpty().MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
    }
}
