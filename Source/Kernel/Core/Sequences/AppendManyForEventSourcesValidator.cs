// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="AppendManyForEventSources"/>.
/// </summary>
internal class AppendManyForEventSourcesValidator : CommandValidator<AppendManyForEventSources>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendManyForEventSourcesValidator"/> class.
    /// </summary>
    public AppendManyForEventSourcesValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
        RuleFor(_ => _.EventSequenceId).NotEmpty().WithMessage("Event sequence identifier is required.");

        RuleFor(_ => _.Events)
            .Must((command, events) => events is not null && (events.Any() || command.ConcurrencyScopes?.Any() == true))
            .WithMessage("At least one event is required.");

        // Omitted or empty routing metadata is accepted per event. The handler resolves it to
        // canonical append defaults before validating constraints or persisting the batch.
        RuleForEach(_ => _.Events).ChildRules(@event =>
        {
            @event.RuleFor(_ => _.EventSourceId).NotEmpty().WithMessage("Event source identifier is required.");
            @event.RuleFor(_ => _.EventType).NotNull().WithMessage("Event type is required.");
            @event.RuleFor(_ => _.EventType.Id).NotEmpty().When(_ => _.EventType is not null).WithMessage("Event type identifier is required.");
            @event.RuleFor(_ => _.Content).NotNull().WithMessage("Event content is required.");
        });
    }
}
