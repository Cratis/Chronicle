// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the validator for <see cref="AppendManyOnceWithNamedTags"/>.
/// </summary>
internal class AppendManyOnceWithNamedTagsValidator : CommandValidator<AppendManyOnceWithNamedTags>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppendManyOnceWithNamedTagsValidator"/> class.
    /// </summary>
    public AppendManyOnceWithNamedTagsValidator()
    {
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.Namespace).RequiredNamespace();
        RuleFor(_ => _.EventSequenceId).RequiredEventSequence();
        RuleFor(_ => _.Events).RequiredEvents();

        RuleForEach(_ => _.Events).ChildRules(entry =>
        {
            entry.RuleFor(_ => _.IdempotencyKey).ValidIdempotencyKey();
            entry.RuleFor(_ => _.Event.EventSourceId).RequiredEventSource();
            entry.RuleFor(_ => _.Event.EventType).RequiredEventType();
            entry.RuleFor(_ => _.Event.EventType.Id).RequiredEventTypeId().When(_ => _.Event.EventType is not null);
            entry.RuleFor(_ => _.Event.Content).RequiredContent();
            entry.RuleForEach(_ => _.Event.NamedTags).ValidNamedTag();
        });
    }
}
