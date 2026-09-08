// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Represents the validator for <see cref="SeedEvents"/>.
/// </summary>
internal class SeedEventsValidator : CommandValidator<SeedEvents>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeedEventsValidator"/> class.
    /// </summary>
    public SeedEventsValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.GlobalByEventType).NotNull().WithMessage("Global entries by event type are required.");
        RuleFor(_ => _.GlobalByEventSource).NotNull().WithMessage("Global entries by event source are required.");
        RuleFor(_ => _.NamespacedEntries).NotNull().WithMessage("Namespaced entries are required.");
        RuleForEach(_ => _.GlobalByEventType).ChildRules(entries =>
            entries.RuleFor(_ => _.EventTypeId).NotEmpty().WithMessage("Event type identifier is required."));
        RuleForEach(_ => _.GlobalByEventSource).ChildRules(entries =>
            entries.RuleFor(_ => _.EventSourceId).NotEmpty().WithMessage("Event source identifier is required."));
        RuleForEach(_ => _.NamespacedEntries).ChildRules(entries =>
            entries.RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required."));
    }
}
