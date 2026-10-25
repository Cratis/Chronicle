// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="AppendOnceWithNamedTags"/>.
/// </summary>
internal class AppendOnceWithNamedTagsValidator : CommandValidator<AppendOnceWithNamedTags>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendOnceWithNamedTagsValidator"/> class.
    /// </summary>
    public AppendOnceWithNamedTagsValidator()
    {
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.Namespace).RequiredNamespace();
        RuleFor(_ => _.EventSequenceId).RequiredEventSequence();
        RuleFor(_ => _.EventSourceId).RequiredEventSource();
        RuleFor(_ => _.EventType).RequiredEventType();
        RuleFor(_ => _.EventType).RequiredEventTypeIdentifier();
        RuleFor(_ => _.Content).RequiredContent();
        RuleFor(_ => _.IdempotencyKey).RequiredIdempotencyKey();
        RuleForEach(_ => _.NamedTags).ValidNamedTag();
    }
}
