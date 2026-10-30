// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Validates the audit reason accompanying a privilege revocation.
/// </summary>
internal class RevokeAdministratorValidator : CommandValidator<RevokeAdministrator>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevokeAdministratorValidator"/> class.
    /// </summary>
    public RevokeAdministratorValidator() => RuleFor(command => command.Reason).NotEmpty();
}
