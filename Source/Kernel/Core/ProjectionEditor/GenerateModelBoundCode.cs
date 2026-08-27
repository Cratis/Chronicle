// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents a request to generate model-bound C# read model code from projection declaration language.
/// </summary>
/// <param name="EventStore">The event store the projection targets.</param>
/// <param name="Namespace">The namespace the projection targets.</param>
/// <param name="Declaration">The projection declaration language representation of the projection.</param>
/// <param name="DraftReadModel">Optional draft read model definition to use for code generation.</param>
[Command]
public record GenerateModelBoundCode(EventStoreName EventStore, EventStoreNamespaceName Namespace, string Declaration, DraftReadModel? DraftReadModel = null)
{
    /// <summary>
    /// Handles the generate model-bound code request.
    /// </summary>
    /// <param name="projections">The <see cref="IProjections"/> service.</param>
    /// <returns>The generated C# code or errors.</returns>
    /// <exception cref="UnexpectedProjectionResult">Thrown when the projection service returns a result that is neither generated code nor parsing errors.</exception>
    internal async Task<GeneratedCodeResult> Handle(IProjections projections)
    {
        var request = new GenerateModelBoundCodeRequest
        {
            EventStore = EventStore,
            Namespace = Namespace,
            Declaration = Declaration,
            DraftReadModel = DraftReadModel is not null
                ? new DraftReadModelDefinition
                {
                    ContainerName = DraftReadModel.ContainerName,
                    Schema = DraftReadModel.Schema,
                    Identifier = DraftReadModel.Identifier,
                    DisplayName = DraftReadModel.DisplayName
                }
                : null
        };

        var result = await projections.GenerateModelBoundCode(request);

        return result.Value switch
        {
            GeneratedCode code => new GeneratedCodeResult(code.Code, []),
            ProjectionDeclarationParsingErrors errors => new GeneratedCodeResult(
                string.Empty,
                errors.Errors.ToApi()),
            _ => throw new UnexpectedProjectionResult(nameof(GenerateModelBoundCode))
        };
    }
}
