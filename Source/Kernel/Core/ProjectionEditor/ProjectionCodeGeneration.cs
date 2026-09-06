// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using ProjectionCodeLanguage = Cratis.Chronicle.Concepts.Projections.ProjectionCodeLanguage;
using ProjectionOwner = Cratis.Chronicle.Concepts.Projections.ProjectionOwner;

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Generates client code for a projection declaration, shared by the declarative and model-bound commands.
/// </summary>
/// <remarks>
/// The two commands differ only in which style they ask the language service for, so the compile - resolving the
/// read models the declaration may target, standing a draft one in for a read model that does not exist yet, and
/// turning compiler errors into something the editor can point at - lives here once rather than in both.
/// </remarks>
internal static class ProjectionCodeGeneration
{
    /// <summary>
    /// Compiles a declaration and generates the client code for it.
    /// </summary>
    /// <param name="eventStore">The event store the projection targets.</param>
    /// <param name="declaration">The projection declaration language source text.</param>
    /// <param name="draftReadModel">The read model being drafted alongside the projection, if any.</param>
    /// <param name="language">The language to generate for.</param>
    /// <param name="style">The style to generate the projection in.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the read models and event types.</param>
    /// <param name="languageService">The <see cref="ILanguageService"/> to compile and generate with.</param>
    /// <returns>The generated code, or the syntax errors saying why nothing was generated.</returns>
    internal static async Task<GeneratedCodeResult> Generate(
        EventStoreName eventStore,
        string declaration,
        DraftReadModel? draftReadModel,
        ProjectionCodeLanguage language,
        ProjectionCodeStyle style,
        IStorage storage,
        ILanguageService languageService)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var readModels = await eventStoreStorage.ReadModels.GetAll();

        if (draftReadModel is not null)
        {
            var draft = CreateDraftDefinition(draftReadModel);
            readModels = readModels
                .Where(_ => _.Identifier != draft.Identifier)
                .Append(draft)
                .ToList();
        }

        var eventTypeSchemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();

        var compiled = languageService.Compile(declaration ?? string.Empty, ProjectionOwner.Server, readModels, eventTypeSchemas);

        return compiled.Match(
            definition =>
            {
                var readModelDefinition = readModels.FirstOrDefault(_ => _.Identifier == definition.ReadModel);

                if (readModelDefinition is null || readModelDefinition.Schemas.Count == 0)
                {
                    return Failed($"Read model '{definition.ReadModel}' not found");
                }

                var code = style == ProjectionCodeStyle.Declarative
                    ? languageService.GenerateDeclarativeCode(definition, readModelDefinition, language)
                    : languageService.GenerateModelBoundCode(definition, readModelDefinition, language);

                return new GeneratedCodeResult(code, []);
            },
            errors => new GeneratedCodeResult(
                string.Empty,
                errors.Errors.Select(_ => new ProjectionDeclarationSyntaxError(_.Message, _.Line, _.Column))));
    }

    /// <summary>
    /// Reads a language by name, falling back to C#.
    /// </summary>
    /// <param name="language">The language name from the command.</param>
    /// <returns>The language to generate for.</returns>
    /// <remarks>
    /// The language crosses the wire by name rather than as an enum: an enum-typed parameter on a command record
    /// trips the assembly's documentation embedder, and a name survives a client that does not know a value this
    /// server has. An unknown or empty name means C#, which is what a caller that says nothing gets.
    /// </remarks>
    internal static ProjectionCodeLanguage ParseLanguage(string language) =>
        Enum.TryParse<ProjectionCodeLanguage>(language, ignoreCase: true, out var parsed)
            ? parsed
            : ProjectionCodeLanguage.CSharp;

    static GeneratedCodeResult Failed(string message) =>
        new(string.Empty, [new ProjectionDeclarationSyntaxError(message, 1, 1)]);

    static ReadModelDefinition CreateDraftDefinition(DraftReadModel draft)
    {
        var identifier = string.IsNullOrWhiteSpace(draft.Identifier)
            ? $"draft-{Guid.NewGuid()}"
            : draft.Identifier;

        var displayName = string.IsNullOrWhiteSpace(draft.DisplayName)
            ? draft.ContainerName
            : draft.DisplayName;

        var schema = ParseSchema(draft.Schema, displayName);

        if (string.IsNullOrEmpty(schema.Title))
        {
            schema.Title = displayName;
        }

        return new ReadModelDefinition(
            identifier,
            draft.ContainerName,
            displayName,
            ReadModelOwner.Server,
            ReadModelSource.User,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } },
            []);
    }

    static JsonSchema ParseSchema(string schema, string displayName)
    {
        if (string.IsNullOrEmpty(schema))
        {
            return new JsonSchema { Type = JsonObjectType.Object, Title = displayName };
        }

        try
        {
            return JsonSchema.FromJson(schema);
        }
        catch (Exception)
        {
            // A draft schema comes straight from an editor buffer, so it is expected to be unparseable while it
            // is still being typed. An empty object schema is the answer the editor wants, not an error.
            return new JsonSchema { Type = JsonObjectType.Object, Title = displayName };
        }
    }
}
