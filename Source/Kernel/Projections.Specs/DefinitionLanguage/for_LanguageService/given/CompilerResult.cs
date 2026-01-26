// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.given;

public record CompilerResult(ProjectionDefinition Definition, string GeneratedDefinition)
{
    public static implicit operator ProjectionDefinition(CompilerResult result) => result.Definition;
}
