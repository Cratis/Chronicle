// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="AppendManyWithNamedTags"/>.
/// </summary>
/// <remarks>
/// The command hands over to <see cref="AppendMany"/> without going back through the command pipeline, so
/// <see cref="AppendManyValidator"/> never sees it. This validator applies the same rules, plus the named-tag rules.
/// </remarks>
internal class AppendManyWithNamedTagsValidator : CommandValidator<AppendManyWithNamedTags>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendManyWithNamedTagsValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> used to tell whether an event sequence already exists.</param>
    /// <param name="reservedEventSequenceIds">The <see cref="IReservedEventSequenceIds"/> of the active storage provider.</param>
    public AppendManyWithNamedTagsValidator(IStorage storage, IReservedEventSequenceIds reservedEventSequenceIds)
    {
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.EventStore).NoKeySeparator();
        RuleFor(_ => _.Namespace).RequiredNamespace();
        RuleFor(_ => _.Namespace).NoKeySeparator();
        RuleFor(_ => _.EventSequenceId).RequiredEventSequence();
        RuleFor(_ => _.EventSequenceId).NoKeySeparator();
        RuleFor(_ => _.EventSequenceId).NotReservedForNewEventSequence(storage, reservedEventSequenceIds, _ => _.EventStore, _ => _.Namespace);
        RuleFor(_ => _.EventSourceId).RequiredEventSource();
        RuleFor(_ => _.Events).RequiredEvents();
        RuleForEach(_ => _.Events).ChildRules(@event =>
        {
            @event.RuleFor(_ => _.EventType).RequiredEventType();
            @event.RuleFor(_ => _.EventType.Id).RequiredEventTypeId().When(_ => _.EventType is not null);
            @event.RuleFor(_ => _.Content).RequiredContent();
            @event.RuleForEach(_ => _.NamedTags).ValidNamedTag();
        });
    }
}
