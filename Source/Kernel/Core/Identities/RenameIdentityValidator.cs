// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Identities;

/// <summary>
/// Represents the validator for <see cref="RenameIdentity"/>.
/// </summary>
internal class RenameIdentityValidator : CommandValidator<RenameIdentity>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RenameIdentityValidator"/> class.
    /// </summary>
    public RenameIdentityValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.Subject).NotEmpty().WithMessage("Subject is required.");
        RuleFor(_ => _.Name).NotEmpty().WithMessage("Name is required.");
    }
}
