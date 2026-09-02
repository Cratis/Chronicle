// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;

/// <summary>
/// Renders a projection as C# for the .NET client.
/// </summary>
/// <param name="declarative">Generates the declarative form.</param>
/// <param name="modelBound">Generates the model-bound form.</param>
public class CSharpProjectionCodeGenerator(
    DeclarativeCodeGenerator declarative,
    ModelBoundCodeGenerator modelBound) : IProjectionCodeGenerator
{
    /// <inheritdoc/>
    public ProjectionCodeLanguage Language => ProjectionCodeLanguage.CSharp;

    /// <inheritdoc/>
    public bool Supports(ProjectionCodeStyle style) => true;

    /// <inheritdoc/>
    public string GenerateDeclarative(ProjectionDefinition definition, ReadModelDefinition readModelDefinition) =>
        declarative.Generate(definition, readModelDefinition).ToFullString();

    /// <inheritdoc/>
    public string GenerateModelBound(ProjectionDefinition definition, ReadModelDefinition readModelDefinition) =>
        modelBound.Generate(definition, readModelDefinition).ToFullString();
}
