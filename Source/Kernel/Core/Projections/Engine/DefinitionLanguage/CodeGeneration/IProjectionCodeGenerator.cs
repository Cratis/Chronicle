// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;

/// <summary>
/// Defines a generator that renders a projection as client code in one language.
/// </summary>
/// <remarks>
/// One implementation per language, discovered by convention. A language that has a client but no
/// projection API for one of the two styles says so through <see cref="Supports"/> rather than
/// emitting code that would compile against nothing.
/// </remarks>
public interface IProjectionCodeGenerator
{
    /// <summary>
    /// Gets the language this generator renders.
    /// </summary>
    ProjectionCodeLanguage Language { get; }

    /// <summary>
    /// Determines whether this language's client offers the given projection style.
    /// </summary>
    /// <param name="style">The style to check.</param>
    /// <returns>True when the style can be generated, false when the client has no API for it.</returns>
    bool Supports(ProjectionCodeStyle style);

    /// <summary>
    /// Generates a declarative projection - the projection defined separately from the read model.
    /// </summary>
    /// <param name="definition">The projection to generate.</param>
    /// <param name="readModelDefinition">The read model the projection targets.</param>
    /// <returns>The generated source.</returns>
    string GenerateDeclarative(ProjectionDefinition definition, ReadModelDefinition readModelDefinition);

    /// <summary>
    /// Generates a model-bound read model - the projection expressed on the read model itself.
    /// </summary>
    /// <param name="definition">The projection to generate.</param>
    /// <param name="readModelDefinition">The read model the projection targets.</param>
    /// <returns>The generated source.</returns>
    string GenerateModelBound(ProjectionDefinition definition, ReadModelDefinition readModelDefinition);
}
