// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="AppendMany"/>.
/// </summary>
internal class AppendManyValidator : CommandValidator<AppendMany>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendManyValidator"/> class.
    /// </summary>
    public AppendManyValidator()
    {
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.Namespace).RequiredNamespace();
        RuleFor(_ => _.EventSequenceId).RequiredEventSequence();
        RuleFor(_ => _.EventSourceId).RequiredEventSource();
        RuleFor(_ => _.Events)
            .Must((command, events) => events is not null && (events.Any() || (command.ConcurrencyScope is { } scope && (scope.ExpectsNoMatchingEvent || scope.SequenceNumber < Concepts.Events.EventSequenceNumber.BeforeFirst.Value))))
            .WithMessage("At least one event is required.");
        RuleForEach(_ => _.Events).ChildRules(@event =>
        {
            @event.RuleFor(_ => _.EventType).RequiredEventType();
            @event.RuleFor(_ => _.EventType.Id).RequiredEventTypeId().When(_ => _.EventType is not null);
            @event.RuleFor(_ => _.Content).RequiredContent();
        });
    }
}
