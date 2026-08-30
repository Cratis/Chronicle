// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the validator for <see cref="ValidateCaptureDeclaration"/>.
/// </summary>
internal class ValidateCaptureDeclarationValidator : CommandValidator<ValidateCaptureDeclaration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateCaptureDeclarationValidator"/> class.
    /// </summary>
    public ValidateCaptureDeclarationValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Declaration).NotEmpty().WithMessage("Declaration is required.");
    }
}
