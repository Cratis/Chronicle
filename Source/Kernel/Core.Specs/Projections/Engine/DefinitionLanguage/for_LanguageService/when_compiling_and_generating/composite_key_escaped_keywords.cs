// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class composite_key_escaped_keywords : given.a_language_service_with_schemas<given.KeywordKeyReadModel>
{
    const string Declaration = """
        projection MyProjection => KeywordKeyReadModel
          from UserRegisteredWithKeywordValues
            key KeywordKey
              @from = @from
              @projection = @projection
              @key = @key
              @join = @join
              @children = @children
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserRegisteredWithKeywordValues)];

    given.CompilerResult _result;

    void Because() => _result = CompileGenerateAndRecompile(Declaration);

    [Fact] void should_have_one_from_definition() => _result.Definition.From.Count.ShouldEqual(1);
    [Fact] void should_have_from_user_registered() => _result.Definition.From.ContainsKey("UserRegisteredWithKeywordValues").ShouldBeTrue();
    [Fact] void should_have_composite_key_expression() => _result.Definition.From[(EventType)"UserRegisteredWithKeywordValues"].Key.Value.ShouldEqual($"{WellKnownExpressions.Composite}(KeywordKey, from=from, projection=projection, key=key, join=join, children=children)");

    [Fact] void should_regenerate_key_block_with_escaped_from() => _result.GeneratedDefinition.ShouldContain("@from");
    [Fact] void should_regenerate_key_block_with_escaped_projection() => _result.GeneratedDefinition.ShouldContain("@projection");
    [Fact] void should_regenerate_key_block_with_escaped_key() => _result.GeneratedDefinition.ShouldContain("@key");
    [Fact] void should_regenerate_key_block_with_escaped_join() => _result.GeneratedDefinition.ShouldContain("@join");
    [Fact] void should_regenerate_key_block_with_escaped_children() => _result.GeneratedDefinition.ShouldContain("@children");
}
