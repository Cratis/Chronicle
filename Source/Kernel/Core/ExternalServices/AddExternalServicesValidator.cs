// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.ExternalServices;

/// <summary>
/// Represents the validator for <see cref="AddExternalServices"/>.
/// </summary>
internal class AddExternalServicesValidator : CommandValidator<AddExternalServices>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddExternalServicesValidator"/> class.
    /// </summary>
    public AddExternalServicesValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.ExternalServices).NotEmpty().WithMessage("At least one external service is required.");
        RuleForEach(_ => _.ExternalServices).ChildRules(service =>
            service.RuleFor(_ => _.Name).NotEmpty().WithMessage("External service name is required."));
    }
}
