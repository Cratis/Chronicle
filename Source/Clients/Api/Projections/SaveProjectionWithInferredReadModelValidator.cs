// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Contracts.ReadModels;
using FluentValidation;

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a validator for <see cref="SaveProjectionWithInferredReadModel"/>.
/// </summary>
internal class SaveProjectionWithInferredReadModelValidator : CommandValidator<SaveProjectionWithInferredReadModel>
{
    readonly IReadModels _readModels;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveProjectionWithInferredReadModelValidator"/> class.
    /// </summary>
    /// <param name="readModels"><see cref="IReadModels"/> for checking existing read model definitions.</param>
    public SaveProjectionWithInferredReadModelValidator(IReadModels readModels)
    {
        _readModels = readModels;

        RuleFor(_ => _.ReadModelDisplayName)
            .NotEmpty()
            .WithMessage("Read model name is required.");

        RuleFor(_ => _)
            .MustAsync(NotHaveExistingReadModel)
            .WithMessage("A read model with this name already exists.");
    }

    async Task<bool> NotHaveExistingReadModel(SaveProjectionWithInferredReadModel command, CancellationToken cancellationToken)
    {
        var response = await _readModels.GetDefinitions(new GetDefinitionsRequest { EventStore = command.EventStore });
        var identifier = command.ToIdentifier();
        return !response.ReadModels.Any(d => d.Type?.Identifier == identifier);
    }
}
