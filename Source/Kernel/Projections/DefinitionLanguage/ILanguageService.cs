// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Defines a language service for parsing and generating projection DSL.
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Parses a language definition string into a ProjectionDefinition.
    /// </summary>
    /// <param name="dsl">The DSL string to parse.</param>
    /// <param name="identifier">The projection identifier.</param>
    /// <param name="owner">The projection owner.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>A ProjectionDefinition.</returns>
    ProjectionDefinition Compile(string dsl, ProjectionId identifier, ProjectionOwner owner, EventSequenceId eventSequenceId);

    /// <summary>
    /// Generates a language definition string from a ProjectionDefinition.
    /// </summary>
    /// <param name="definition">The ProjectionDefinition to generate from.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <returns>The generated language definition string.</returns>
    string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition);
}
