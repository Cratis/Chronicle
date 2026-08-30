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
        // Name is the event store being created, not a reference to an existing one, so EventStoreNameValidator
        // is deliberately not attached here.
        RuleFor(_ => _.Name).NotEmpty().WithMessage("Event store name is required.");
    }
}
