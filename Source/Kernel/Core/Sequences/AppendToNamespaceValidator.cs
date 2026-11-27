// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>Validates a protected single append without weakening the existing append rules.</summary>
internal class AppendToNamespaceValidator : CommandValidator<AppendToNamespace>
{
    /// <summary>Initializes a new instance of the <see cref="AppendToNamespaceValidator"/> class.</summary>
    public AppendToNamespaceValidator()
    {
        RuleFor(command => command.EventStore).NotSystemEventStore();
        RuleFor(command => command.ExpectedIncarnation).Must(value => value != Guid.Empty).WithMessage("A captured namespace identity is required.");
        RuleFor(command => command.ToAppend(command.Namespace)).SetValidator(new AppendValidator());
        RuleForEach(command => command.NamedTags).ValidNamedTag();
    }
}
