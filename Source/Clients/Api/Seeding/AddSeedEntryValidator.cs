// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Api.Seeding;

/// <summary>
/// Represents a validator for <see cref="AddSeedEntry"/>.
/// </summary>
internal class AddSeedEntryValidator : CommandValidator<AddSeedEntry>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddSeedEntryValidator"/> class.
    /// </summary>
    public AddSeedEntryValidator()
    {
        RuleFor(_ => _.EventStore)
            .NotEmpty()
            .WithMessage("Event store is required.");

        RuleFor(_ => _.EventSourceId)
            .NotEmpty()
            .WithMessage("Event source identifier is required.");

        RuleFor(_ => _.EventTypeId)
            .NotEmpty()
            .WithMessage("Event type identifier is required.");

        RuleFor(_ => _.Content)
            .NotEmpty()
            .WithMessage("Event content is required.");
    }
}
