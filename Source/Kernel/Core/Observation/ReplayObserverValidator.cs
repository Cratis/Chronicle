// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the validator for <see cref="ReplayObserver"/>.
/// </summary>
internal class ReplayObserverValidator : CommandValidator<ReplayObserver>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayObserverValidator"/> class.
    /// </summary>
    public ReplayObserverValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.ObserverId).NotEmpty().WithMessage("Observer identifier is required.");
    }
}
