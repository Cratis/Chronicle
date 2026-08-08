// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the validator for <see cref="RequirePasswordChange"/>.
/// </summary>
internal class RequirePasswordChangeValidator : CommandValidator<RequirePasswordChange>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePasswordChangeValidator"/> class.
    /// </summary>
    public RequirePasswordChangeValidator()
    {
        RuleFor(_ => _.UserId).NotEmpty().WithMessage("User identifier is required.");
    }
}
