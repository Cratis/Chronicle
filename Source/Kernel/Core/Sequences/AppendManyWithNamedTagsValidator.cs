// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
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
    public AppendManyWithNamedTagsValidator()
    {
        RuleFor(_ => _.EventStore).RequiredEventStore();
        RuleFor(_ => _.Namespace).RequiredNamespace();
        RuleFor(_ => _.EventSequenceId).RequiredEventSequence();
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
