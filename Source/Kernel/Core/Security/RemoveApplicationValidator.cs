// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the validator for <see cref="RemoveApplication"/>.
/// </summary>
internal class RemoveApplicationValidator : CommandValidator<RemoveApplication>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveApplicationValidator"/> class.
    /// </summary>
    public RemoveApplicationValidator()
    {
        RuleFor(_ => _.Id).NotEmpty().WithMessage("Application identifier is required.");
    }
}
