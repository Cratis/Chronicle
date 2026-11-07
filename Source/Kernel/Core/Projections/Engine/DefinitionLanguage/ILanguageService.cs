// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Defines a language service for parsing and generating projection declaration language.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Parses a language definition string into a ProjectionDefinition.
    /// </summary>
    /// <param name="definition">The definition string to parse.</param>
    /// <param name="owner">The projection owner.</param>
    /// <param name="readModelDefinitions">Available read model definitions for validation.</param>
    /// <param name="eventTypeSchemas">Available event type schemas for validation.</param>
    /// <param name="generationDelivery">The delivery mode; Pinned resolves bare event names to their latest registered generation.</param>
    /// <returns>A ProjectionDefinition or compiler errors.</returns>
    Result<ProjectionDefinition, CompilerErrors> Compile(
        string definition,
        ProjectionOwner owner,
        IEnumerable<ReadModelDefinition> readModelDefinitions,
        IEnumerable<EventTypeSchema> eventTypeSchemas,
        EventGenerationDelivery generationDelivery = EventGenerationDelivery.Compatibility);

    /// <summary>
    /// Gets whether a declaration holds a graph projection, 'projection ... => graph ...'.
    /// </summary>
    /// <param name="definition">The declaration text.</param>
    /// <returns>True if the declaration builds a graph.</returns>
    bool IsGraphDeclaration(string definition);

    /// <summary>
    /// Compiles a graph declaration, the 'graph' and the graph projection that builds it, into the projection
    /// definition and the read model that holds the graph.
    /// </summary>
    /// <param name="definition">The declaration text.</param>
    /// <param name="owner">The <see cref="ProjectionOwner"/> of the projection.</param>
    /// <param name="eventTypeSchemas">The event type schemas used to resolve events and map properties by name.</param>
    /// <param name="generationDelivery">The <see cref="EventGenerationDelivery"/>.</param>
    /// <returns>The <see cref="CompiledGraph"/>, or the compiler errors.</returns>
    Result<CompiledGraph, CompilerErrors> CompileGraph(
        string definition,
        ProjectionOwner owner,
        IEnumerable<EventTypeSchema> eventTypeSchemas,
        EventGenerationDelivery generationDelivery = EventGenerationDelivery.Compatibility);

    /// <summary>
    /// Generates a language definition string from a ProjectionDefinition.
    /// </summary>
    /// <param name="definition">The ProjectionDefinition to generate from; a graph projection generates the graph and its projection.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <returns>The generated language definition string.</returns>
    string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition);

    /// <summary>
    /// Gets the read model identifier from a definition string.
    /// </summary>
    /// <param name="definition">The definition string to extract from.</param>
    /// <returns>The read model identifier or compiler errors.</returns>
    Result<ReadModelIdentifier, CompilerErrors> GetReadModelIdentifier(string definition);

    /// <summary>
    /// Generates declarative projection code from a projection definition.
    /// </summary>
    /// <param name="definition">The ProjectionDefinition to generate code from.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <param name="language">The language to generate for. Defaults to C#.</param>
    /// <returns>The generated code for a declarative projection.</returns>
    string GenerateDeclarativeCode(ProjectionDefinition definition, ReadModelDefinition readModelDefinition, ProjectionCodeLanguage language = ProjectionCodeLanguage.CSharp);

    /// <summary>
    /// Generates model-bound read model code from a projection definition.
    /// </summary>
    /// <param name="definition">The ProjectionDefinition to generate code from.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <param name="language">The language to generate for. Defaults to C#.</param>
    /// <returns>The generated code for a model-bound read model.</returns>
    string GenerateModelBoundCode(ProjectionDefinition definition, ReadModelDefinition readModelDefinition, ProjectionCodeLanguage language = ProjectionCodeLanguage.CSharp);

    /// <summary>
    /// Gets the languages that can generate the given projection style.
    /// </summary>
    /// <param name="style">The style to check.</param>
    /// <returns>The languages whose client offers an API for that style.</returns>
    IEnumerable<ProjectionCodeLanguage> GetLanguagesSupporting(ProjectionCodeStyle style);
}
