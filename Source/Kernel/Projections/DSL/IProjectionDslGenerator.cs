// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.DSL;

/// <summary>
/// Defines a generator that can produce a projection DSL string from a <see cref="ProjectionDefinition"/>.
/// </summary>
public interface IProjectionDslGenerator
{
    /// <summary>
    /// Generate DSL for the supplied projection definition.
    /// </summary>
    /// <param name="definition">Projection definition to generate DSL for.</param>
    /// <param name="readModelDefinition">The read model definition the projection targets.</param>
    /// <returns>Generated DSL string.</returns>
    string Generate(ProjectionDefinition definition, ReadModelDefinition readModelDefinition);
}
