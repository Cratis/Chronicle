// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// Represents a request to generate model-bound C# read model code from projection declaration language.
/// </summary>
/// <param name="EventStore">The event store the projection targets.</param>
/// <param name="Namespace">The namespace the projection targets.</param>
/// <param name="Declaration">The projection declaration language representation of the projection.</param>
/// <param name="DraftReadModel">Optional draft read model definition to use for code generation.</param>
/// <param name="Language">The language to generate for - one of the <see cref="ProjectionCodeLanguage"/> names. Defaults to C#.</param>
[Command]
public record GenerateModelBoundCode(string EventStore, string Namespace, string Declaration, DraftReadModel? DraftReadModel = null, string Language = nameof(ProjectionCodeLanguage.CSharp))
{
    /// <summary>
    /// Handles the generate model-bound code request.
    /// </summary>
    /// <param name="projections">The <see cref="IProjections"/> service.</param>
    /// <returns>The generated C# code or errors.</returns>
    public async Task<GeneratedCodeResult> Handle(IProjections projections)
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
                : null,
            Language = ParseLanguage(Language)
        };

        var result = await projections.GenerateModelBoundCode(request);

        return result.Value switch
        {
            GeneratedCode code => new GeneratedCodeResult(code.Code, []),
            ProjectionDeclarationParsingErrors errors => new GeneratedCodeResult(
                string.Empty,
                errors.Errors.ToApi()),
            _ => throw new UnexpectedProjectionResult(nameof(GenerateModelBoundCode), result.GetType())
        };
    }

    /// <summary>
    /// Reads the requested language, falling back to C#.
    /// </summary>
    /// <param name="language">The language name from the request.</param>
    /// <returns>The language to generate for.</returns>
    /// <remarks>
    /// The language crosses the wire by name rather than as an enum: an enum-typed parameter on a
    /// command record trips the assembly's documentation embedder, and a name survives a client that
    /// does not know a value this server has. An unknown or empty name means C#, which is what a
    /// caller that says nothing gets.
    /// </remarks>
    static ProjectionCodeLanguage ParseLanguage(string language) =>
        Enum.TryParse<ProjectionCodeLanguage>(language, ignoreCase: true, out var parsed)
            ? parsed
            : ProjectionCodeLanguage.CSharp;
}
