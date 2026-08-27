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
        // Name is the event store being created, not a reference to one that must already exist - the
        // cross-cutting EventStoreNameValidator existence check does not apply here.
        RuleFor(_ => _.Name).IgnoreConceptRules().NotEmpty().WithMessage("Event store name is required.");
    }
}
