// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Chronicle.ReadModelDefinitions;

/// <summary>
/// Represents the validator for <see cref="UpdateReadModelDefinition"/>.
/// </summary>
internal class UpdateReadModelDefinitionValidator : CommandValidator<UpdateReadModelDefinition>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateReadModelDefinitionValidator"/> class.
    /// </summary>
    public UpdateReadModelDefinitionValidator()
    {
        RuleFor(_ => _.EventStore).NotEmpty().WithMessage("Event store name is required.");
        RuleFor(_ => _.Identifier).NotEmpty().WithMessage("Identifier is required.");
        RuleFor(_ => _.ContainerName).NotEmpty().WithMessage("Container name is required.");
        RuleFor(_ => _.Schema).NotEmpty().WithMessage("Schema is required.");
        RuleFor(_ => _.Indexes).NotNull().WithMessage("Indexes are required.");
        RuleForEach(_ => _.Indexes).NotEmpty().WithMessage("Index name cannot be empty.");
    }
}
