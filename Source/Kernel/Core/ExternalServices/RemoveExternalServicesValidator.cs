// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts.ExternalServices;
using FluentValidation;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents the validator for <see cref="RemoveExternalServices"/>.
/// </summary>
internal class RemoveExternalServicesValidator : CommandValidator<RemoveExternalServices>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveExternalServicesValidator"/> class.
    /// </summary>
    public RemoveExternalServicesValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.ExternalServices).NotEmpty().WithMessage("At least one external service is required.");

        // RuleForEach's own NotEmpty() checks the element reference against null/default, not the wrapped
        // value - ExternalServiceId is a non-null record even when its Value is empty, so the check has to be
        // written against the concept's own sentinel instead.
        RuleForEach(_ => _.ExternalServices).Must(id => id != ExternalServiceId.Unspecified).WithMessage("External service identifier cannot be empty.");
    }
}
