// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class escaped_keywords : given.a_language_service_with_schemas<given.ModelWithKeywordProperties>
{
    const string Declaration = """
        projection MyProjection => ModelWithKeywordProperties
          no automap
          from UserRegisteredWithKeywordValues
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
    [Fact] void should_have_five_property_mappings() => _result.Definition.From[(EventType)"UserRegisteredWithKeywordValues"].Properties.Count.ShouldEqual(5);
    [Fact] void should_map_from_property() => _result.Definition.From[(EventType)"UserRegisteredWithKeywordValues"].Properties[new PropertyPath("from")].ShouldEqual("from");
    [Fact] void should_map_projection_property() => _result.Definition.From[(EventType)"UserRegisteredWithKeywordValues"].Properties[new PropertyPath("projection")].ShouldEqual("projection");
    [Fact] void should_map_key_property() => _result.Definition.From[(EventType)"UserRegisteredWithKeywordValues"].Properties[new PropertyPath("key")].ShouldEqual("key");
    [Fact] void should_map_join_property() => _result.Definition.From[(EventType)"UserRegisteredWithKeywordValues"].Properties[new PropertyPath("join")].ShouldEqual("join");
    [Fact] void should_map_children_property() => _result.Definition.From[(EventType)"UserRegisteredWithKeywordValues"].Properties[new PropertyPath("children")].ShouldEqual("children");
    [Fact] void should_regenerate_with_escaped_from() => _result.GeneratedDefinition.ShouldContain("@from");
    [Fact] void should_regenerate_with_escaped_projection() => _result.GeneratedDefinition.ShouldContain("@projection");
    [Fact] void should_regenerate_with_escaped_key() => _result.GeneratedDefinition.ShouldContain("@key");
    [Fact] void should_regenerate_with_escaped_join() => _result.GeneratedDefinition.ShouldContain("@join");
    [Fact] void should_regenerate_with_escaped_children() => _result.GeneratedDefinition.ShouldContain("@children");
}
