// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Validates a request to initialize observers.
/// </summary>
internal class InitializeObserversValidator : CommandValidator<InitializeObservers>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InitializeObserversValidator"/> class.
    /// </summary>
    public InitializeObserversValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
    }
}
