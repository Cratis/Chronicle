// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Api.Namespaces;

/// <summary>
/// Represents a validator for <see cref="Ensure"/>.
/// </summary>
internal class EnsureValidator : CommandValidator<Ensure>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnsureValidator"/> class.
    /// </summary>
    public EnsureValidator()
    {
        RuleFor(_ => _.Namespace)
            .NotEmpty()
            .WithMessage("Namespace is required.");
    }
}
