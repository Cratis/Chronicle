// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class projection_with_automap : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Declaration = """
        projection User => UserReadModel
          automap
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserCreated)];

    given.CompilerResult _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_be_valid_definition() => _result.Definition.ShouldNotBeNull();
    [Fact] void should_have_automap_enabled() => _result.Definition.AutoMap.ShouldEqual(AutoMap.Enabled);
    [Fact] void should_not_contain_any_simple_mappings_in_generated_projection_definition_language() => _result.GeneratedDefinition.ShouldNotContain(" = ");
}
