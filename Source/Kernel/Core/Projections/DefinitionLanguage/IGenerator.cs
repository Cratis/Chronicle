// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.DefinitionLanguage;

/// <summary>
/// Defines functionality for generating projection language definition strings from ProjectionDefinition objects.
/// </summary>
public interface IGenerator
{
    /// <summary>
    /// Generate language definition string for the supplied projection definition.
    /// </summary>
    /// <param name="definition">Projection definition to generate projection declaration language for.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <returns>Generated projection declaration language string.</returns>
    string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition);
}
