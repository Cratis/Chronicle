// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the validator for <see cref="EnsureEventStore"/>.
/// </summary>
internal class EnsureEventStoreValidator : CommandValidator<EnsureEventStore>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnsureEventStoreValidator"/> class.
    /// </summary>
    public EnsureEventStoreValidator()
    {
        RuleFor(_ => _.Name).NotEmpty().WithMessage("Event store name is required.");
    }
}
