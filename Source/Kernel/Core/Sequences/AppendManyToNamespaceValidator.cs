// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Sequences;

/// <summary>Validates a protected batch without weakening the existing append or named-tag rules.</summary>
internal class AppendManyToNamespaceValidator : CommandValidator<AppendManyToNamespace>
{
    /// <summary>Initializes a new instance of the <see cref="AppendManyToNamespaceValidator"/> class.</summary>
    public AppendManyToNamespaceValidator()
    {
        RuleFor(command => command.ExpectedIncarnation).Must(value => value != Guid.Empty).WithMessage("A captured namespace identity is required.");
        RuleFor(command => command.ToAppend(command.Namespace)).SetValidator(new AppendManyWithNamedTagsValidator());
    }
}
