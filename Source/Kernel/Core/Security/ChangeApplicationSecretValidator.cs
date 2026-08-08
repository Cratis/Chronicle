// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the validator for <see cref="ChangeApplicationSecret"/>.
/// </summary>
internal class ChangeApplicationSecretValidator : CommandValidator<ChangeApplicationSecret>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeApplicationSecretValidator"/> class.
    /// </summary>
    public ChangeApplicationSecretValidator()
    {
        RuleFor(_ => _.Id).NotEmpty().WithMessage("Application identifier is required.");
        RuleFor(_ => _.ClientSecret).NotEmpty().WithMessage("Client secret is required.");
    }
}
