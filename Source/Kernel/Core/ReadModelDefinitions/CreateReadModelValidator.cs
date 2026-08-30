// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.ReadModelDefinitions;

/// <summary>
/// Represents the validator for <see cref="CreateReadModel"/>.
/// </summary>
internal class CreateReadModelValidator : CommandValidator<CreateReadModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateReadModelValidator"/> class.
    /// </summary>
    public CreateReadModelValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Identifier).NotEmpty().WithMessage("Identifier is required.");
        RuleFor(_ => _.DisplayName).NotEmpty().WithMessage("Display name is required.");
        RuleFor(_ => _.ContainerName).NotEmpty().WithMessage("Container name is required.");
    }
}
