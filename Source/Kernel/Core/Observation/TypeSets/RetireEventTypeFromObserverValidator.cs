// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Validates a request to retire an event type.
/// </summary>
internal class RetireEventTypeFromObserverValidator : CommandValidator<RetireEventTypeFromObserver>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetireEventTypeFromObserverValidator"/> class.
    /// </summary>
    public RetireEventTypeFromObserverValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.ObserverId).NotEmpty().WithMessage("Observer identifier is required.");
        RuleFor(_ => _.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");
        RuleFor(_ => _.EventTypeId).NotEmpty().WithMessage("Event type identifier is required.");
    }
}
