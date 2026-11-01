// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="AppendWithNamedTags"/>.
/// </summary>
/// <remarks>
/// The command hands over to <see cref="Append"/> without going back through the command pipeline, so
/// <see cref="AppendValidator"/> never sees it. This validator applies the same rules, plus the named-tag rules.
/// </remarks>
internal class AppendWithNamedTagsValidator : CommandValidator<AppendWithNamedTags>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendWithNamedTagsValidator"/> class.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> used to tell whether an event sequence already exists.</param>
    /// <param name="reservedEventSequenceIds">The <see cref="IReservedEventSequenceIds"/> of the active storage provider.</param>
    public AppendWithNamedTagsValidator(IStorage storage, IReservedEventSequenceIds reservedEventSequenceIds)
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

        // Omitted or empty routing metadata is accepted for older clients and direct API callers.
        // The handler resolves it to canonical append defaults before validating constraints or persisting.
        RuleFor(_ => _.EventType).RequiredEventType();
        RuleFor(_ => _.EventType).RequiredEventTypeIdentifier();
        RuleFor(_ => _.Content).RequiredContent();
        RuleForEach(_ => _.NamedTags).ValidNamedTag();
    }
}
