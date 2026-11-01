// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="AppendManyOnce"/>.
/// </summary>
internal class AppendManyOnceValidator : CommandValidator<AppendManyOnce>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendManyOnceValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> used to tell whether an event sequence already exists.</param>
    /// <param name="reservedEventSequenceIds">The <see cref="IReservedEventSequenceIds"/> of the active storage provider.</param>
    public AppendManyOnceValidator(IStorage storage, IReservedEventSequenceIds reservedEventSequenceIds)
    {
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.EventStore).NoKeySeparator();
        RuleFor(_ => _.Namespace).RequiredNamespace();
        RuleFor(_ => _.Namespace).NoKeySeparator();
        RuleFor(_ => _.EventSequenceId).RequiredEventSequence();
        RuleFor(_ => _.EventSequenceId).NoKeySeparator();
        RuleFor(_ => _.EventSequenceId).NotReservedForNewEventSequence(storage, reservedEventSequenceIds, _ => _.EventStore, _ => _.Namespace);
        RuleFor(_ => _.Events).RequiredEvents();

        RuleForEach(_ => _.Events).ChildRules(entry =>
        {
            entry.RuleFor(_ => _.IdempotencyKey).ValidIdempotencyKey();
            entry.RuleFor(_ => _.Event.EventSourceId).RequiredEventSource();
            entry.RuleFor(_ => _.Event.EventType).RequiredEventType();
            entry.RuleFor(_ => _.Event.EventType.Id).RequiredEventTypeId().When(_ => _.Event.EventType is not null);
            entry.RuleFor(_ => _.Event.Content).RequiredContent();
        });
    }
}
