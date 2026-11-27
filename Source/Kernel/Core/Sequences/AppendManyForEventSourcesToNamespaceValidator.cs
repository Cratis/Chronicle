// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>Validates a protected multi-source batch without weakening the existing append or named-tag rules.</summary>
internal class AppendManyForEventSourcesToNamespaceValidator : CommandValidator<AppendManyForEventSourcesToNamespace>
{
    /// <summary>Initializes a new instance of the <see cref="AppendManyForEventSourcesToNamespaceValidator"/> class.</summary>
    public AppendManyForEventSourcesToNamespaceValidator()
    {
        RuleFor(command => command.EventStore).NotSystemEventStore();
        RuleFor(command => command.ExpectedIncarnation).Must(value => value != Guid.Empty).WithMessage("A captured namespace identity is required.");
        RuleFor(command => command.ToAppend(command.Namespace)).SetValidator(new AppendManyForEventSourcesWithNamedTagsValidator());
    }
}
