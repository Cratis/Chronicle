// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class projection_with_automap : given.a_language_service
{
    const string definition = """
        projection User => UserReadModel
          automap
        """;

    ProjectionDefinition _result;

    void Because() => _result = CompileGenerateAndRecompile(definition, "UserReadModel");

    [Fact] void should_be_valid_definition() => _result.ShouldNotBeNull();
}
