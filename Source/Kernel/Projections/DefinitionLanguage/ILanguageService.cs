// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Defines a language service for parsing and generating projection DSL.
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
    /// <returns>A ProjectionDefinition or compiler errors.</returns>
    Result<ProjectionDefinition, CompilerErrors> Compile(
        string definition,
        ProjectionOwner owner,
        IEnumerable<ReadModelDefinition> readModelDefinitions,
        IEnumerable<EventTypeSchema> eventTypeSchemas);

    /// <summary>
    /// Generates a language definition string from a ProjectionDefinition.
    /// </summary>
    /// <param name="definition">The ProjectionDefinition to generate from.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <returns>The generated language definition string.</returns>
    string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition);

    /// <summary>
    /// Gets the read model identifier from a definition string.
    /// </summary>
    /// <param name="definition">The definition string to extract from.</param>
    /// <returns>The read model identifier or compiler errors.</returns>
    Result<ReadModelIdentifier, CompilerErrors> GetReadModelIdentifier(string definition);
}
