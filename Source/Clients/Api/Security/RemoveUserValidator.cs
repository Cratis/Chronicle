// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents a validator for <see cref="RemoveUser"/>.
/// </summary>
internal class RemoveUserValidator : CommandValidator<RemoveUser>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveUserValidator"/> class.
    /// </summary>
    public RemoveUserValidator()
    {
        RuleFor(_ => _.UserId)
            .NotEmpty()
            .WithMessage("User identifier is required.");
    }
}
