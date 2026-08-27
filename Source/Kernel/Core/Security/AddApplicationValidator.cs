// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the validator for <see cref="AddApplication"/>.
/// </summary>
internal class AddApplicationValidator : CommandValidator<AddApplication>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddApplicationValidator"/> class.
    /// </summary>
    public AddApplicationValidator()
    {
        // Id is the application being created, not a reference to one that must already exist - the
        // cross-cutting ApplicationIdValidator existence check does not apply here.
        RuleFor(_ => _.Id).IgnoreConceptRules().NotEmpty().WithMessage("Application identifier is required.");
        RuleFor(_ => _.ClientId).NotEmpty().WithMessage("Client identifier is required.");
        RuleFor(_ => _.ClientSecret).NotEmpty().WithMessage("Client secret is required.");
    }
}
