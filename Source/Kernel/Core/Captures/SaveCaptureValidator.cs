// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Represents the validator for <see cref="SaveCapture"/>.
/// </summary>
/// <remarks>
/// The identifier is deliberately not required - an empty identifier means a new capture is created.
/// </remarks>
internal class SaveCaptureValidator : CommandValidator<SaveCapture>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SaveCaptureValidator"/> class.
    /// </summary>
    public SaveCaptureValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Declaration).NotEmpty().WithMessage("Declaration is required.");
    }
}
