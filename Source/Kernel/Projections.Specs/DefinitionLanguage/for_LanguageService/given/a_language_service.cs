// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.given;

public class a_language_service : Specification
{
    protected ProjectionDefinition Compile(string definition)
    {
        var languageService = new LanguageService(Substitute.For<IGenerator>());
        var result = languageService.Compile(
            definition,
            new ProjectionId(Guid.NewGuid().ToString()),
            ProjectionOwner.Client,
            EventSequenceId.Log);
        return result.Match(
            projectionDef => projectionDef,
            errors => throw new InvalidOperationException($"Compilation failed: {string.Join(", ", errors.Errors)}"));
    }
}
