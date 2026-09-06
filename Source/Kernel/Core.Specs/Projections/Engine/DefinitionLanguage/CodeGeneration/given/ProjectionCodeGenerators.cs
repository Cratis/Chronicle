// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.CSharp;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.Elixir;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.Kotlin;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.TypeScript;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration.given;

/// <summary>
/// The real generators, as the running kernel discovers them, for specs that exercise the seam
/// rather than one language.
/// </summary>
public static class ProjectionCodeGenerators
{
    /// <summary>
    /// Gets every generator, as an <see cref="IInstancesOf{T}"/> for the language service to take.
    /// </summary>
    /// <returns>The generators.</returns>
    public static IInstancesOf<IProjectionCodeGenerator> All() =>
        new KnownInstancesOf<IProjectionCodeGenerator>(
            new CSharpProjectionCodeGenerator(new DeclarativeCodeGenerator(), new ModelBoundCodeGenerator()),
            new TypeScriptProjectionCodeGenerator(),
            new KotlinProjectionCodeGenerator(),
            new ElixirProjectionCodeGenerator());
}
