// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents the validator for <see cref="StopJob"/>.
/// </summary>
internal class StopJobValidator : CommandValidator<StopJob>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StopJobValidator"/> class.
    /// </summary>
    public StopJobValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.JobId).NotEmpty().WithMessage("Job identifier is required.");
    }
}
