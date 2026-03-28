// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a validator for <see cref="GenerateModelBoundCode"/>.
/// </summary>
internal class GenerateModelBoundCodeValidator : CommandValidator<GenerateModelBoundCode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateModelBoundCodeValidator"/> class.
    /// </summary>
    public GenerateModelBoundCodeValidator()
    {
        RuleFor(_ => _.EventStore)
            .NotEmpty()
            .WithMessage("Event store is required.");

        RuleFor(_ => _.Namespace)
            .NotEmpty()
            .WithMessage("Namespace is required.");

        RuleFor(_ => _.Declaration)
            .NotEmpty()
            .WithMessage("Projection declaration is required.");
    }
}
