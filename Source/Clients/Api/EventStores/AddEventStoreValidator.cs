// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Api.EventStores;

/// <summary>
/// Represents a validator for <see cref="AddEventStore"/>.
/// </summary>
internal class AddEventStoreValidator : CommandValidator<AddEventStore>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddEventStoreValidator"/> class.
    /// </summary>
    public AddEventStoreValidator()
    {
        RuleFor(_ => _.Name)
            .NotEmpty()
            .WithMessage("Event store name is required.");
    }
}
