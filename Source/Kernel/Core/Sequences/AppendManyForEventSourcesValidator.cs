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
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.Namespace).RequiredNamespace();
        RuleFor(_ => _.EventSequenceId).RequiredEventSequence();

        RuleFor(_ => _.Events)
            .Must((command, events) => events is not null && (events.Any() || command.ConcurrencyScopes?.Any(_ => !string.IsNullOrWhiteSpace(_.EventSourceId) && _.Scope is { } scope && (scope.ExpectsNoMatchingEvent || scope.SequenceNumber < Concepts.Events.EventSequenceNumber.BeforeFirst.Value)) == true))
            .WithMessage("At least one event is required.");

        // Omitted or empty routing metadata is accepted per event. The handler resolves it to
        // canonical append defaults before validating constraints or persisting the batch.
        RuleForEach(_ => _.Events).ChildRules(@event =>
        {
            @event.RuleFor(_ => _.EventSourceId).RequiredEventSource();
            @event.RuleFor(_ => _.EventType).RequiredEventType();
            @event.RuleFor(_ => _.EventType.Id).RequiredEventTypeId().When(_ => _.EventType is not null);
            @event.RuleFor(_ => _.Content).RequiredContent();
        });
    }
}
