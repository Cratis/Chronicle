// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents the validator for <see cref="EnsureNamespace"/>.
/// </summary>
internal class EnsureNamespaceValidator : CommandValidator<EnsureNamespace>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnsureNamespaceValidator"/> class.
    /// </summary>
    public EnsureNamespaceValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Namespace).NotEmpty().WithMessage("Namespace name is required.");
    }
}
