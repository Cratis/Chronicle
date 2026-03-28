// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.Api.ReadModelTypes;

/// <summary>
/// Represents a validator for <see cref="UpdateReadModelDefinition"/>.
/// </summary>
internal class UpdateReadModelDefinitionValidator : CommandValidator<UpdateReadModelDefinition>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateReadModelDefinitionValidator"/> class.
    /// </summary>
    public UpdateReadModelDefinitionValidator()
    {
        RuleFor(_ => _.Identifier)
            .NotEmpty()
            .WithMessage("Read model identifier is required.");

        RuleFor(_ => _.ContainerName)
            .NotEmpty()
            .WithMessage("Read model container name is required.");

        RuleFor(_ => _.Schema)
            .NotEmpty()
            .WithMessage("Read model schema is required.");
    }
}
