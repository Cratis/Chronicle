// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>Validates a protected single append without weakening existing keyed or named-tag rules.</summary>
internal class AppendOnceToNamespaceValidator : CommandValidator<AppendOnceToNamespace>
{
    /// <summary>Initializes a new instance of the <see cref="AppendOnceToNamespaceValidator"/> class.</summary>
    public AppendOnceToNamespaceValidator()
    {
        RuleFor(command => command.EventStore).NotSystemEventStore();
        RuleFor(command => command.ExpectedIncarnation).Must(value => value != Guid.Empty).WithMessage("A captured namespace identity is required.");
        RuleFor(command => command.ToAppend(command.Namespace)).SetValidator(new AppendOnceWithNamedTagsValidator());
    }
}
