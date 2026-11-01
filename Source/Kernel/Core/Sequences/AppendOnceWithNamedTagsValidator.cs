// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="AppendOnceWithNamedTags"/>.
/// </summary>
internal class AppendOnceWithNamedTagsValidator : CommandValidator<AppendOnceWithNamedTags>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendOnceWithNamedTagsValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> used to tell whether an event sequence already exists.</param>
    /// <param name="reservedEventSequenceIds">The <see cref="IReservedEventSequenceIds"/> of the active storage provider.</param>
    public AppendOnceWithNamedTagsValidator(IStorage storage, IReservedEventSequenceIds reservedEventSequenceIds)
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
        RuleFor(_ => _.EventType).RequiredEventType();
        RuleFor(_ => _.EventType).RequiredEventTypeIdentifier();
        RuleFor(_ => _.Content).RequiredContent();
        RuleFor(_ => _.IdempotencyKey).RequiredIdempotencyKey();
        RuleForEach(_ => _.NamedTags).ValidNamedTag();
    }
}
