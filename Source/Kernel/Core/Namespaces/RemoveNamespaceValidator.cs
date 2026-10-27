// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Sequences;
using FluentValidation;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents the validator for <see cref="RemoveNamespace"/>.
/// </summary>
internal class RemoveNamespaceValidator : CommandValidator<RemoveNamespace>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveNamespaceValidator"/> class.
    /// </summary>
    public RemoveNamespaceValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.EventStore).NotSystemEventStore();
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
    }
}
