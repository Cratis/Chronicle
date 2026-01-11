// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.DefinitionLanguage.for_LanguageService.when_compiling_and_generating;

public class escaped_keywords : given.a_language_service_with_schemas<given.ModelWithKeywordProperties>
{
    const string Definition = """
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

    ProjectionDefinition _result;
    string _generated;

    void Because()
    {
        var compilationResult = _languageService.Compile(Definition, ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas);
        if (!compilationResult.IsSuccess)
        {
            var errors = compilationResult.AsT1;
            throw new InvalidOperationException($"Compilation failed with {errors.Errors.Count} errors: {string.Join(", ", errors.Errors)}");
        }
        _result = compilationResult.AsT0;
        _generated = _languageService.Generate(_result, _readModelDefinition);
    }

    [Fact] void should_have_one_from_definition() => _result.From.Count.ShouldEqual(1);
    [Fact] void should_have_from_user_registered() => _result.From.ContainsKey("UserRegisteredWithKeywordValues").ShouldBeTrue();
    [Fact] void should_have_five_property_mappings() => _result.From[(EventType)"UserRegisteredWithKeywordValues"].Properties.Count.ShouldEqual(5);
    [Fact] void should_map_from_property() => _result.From[(EventType)"UserRegisteredWithKeywordValues"].Properties[new PropertyPath("from")].ShouldEqual("from");
    [Fact] void should_map_projection_property() => _result.From[(EventType)"UserRegisteredWithKeywordValues"].Properties[new PropertyPath("projection")].ShouldEqual("projection");
    [Fact] void should_map_key_property() => _result.From[(EventType)"UserRegisteredWithKeywordValues"].Properties[new PropertyPath("key")].ShouldEqual("key");
    [Fact] void should_map_join_property() => _result.From[(EventType)"UserRegisteredWithKeywordValues"].Properties[new PropertyPath("join")].ShouldEqual("join");
    [Fact] void should_map_children_property() => _result.From[(EventType)"UserRegisteredWithKeywordValues"].Properties[new PropertyPath("children")].ShouldEqual("children");
    [Fact] void should_regenerate_with_escaped_from() => _generated.ShouldContain("@from");
    [Fact] void should_regenerate_with_escaped_projection() => _generated.ShouldContain("@projection");
    [Fact] void should_regenerate_with_escaped_key() => _generated.ShouldContain("@key");
    [Fact] void should_regenerate_with_escaped_join() => _generated.ShouldContain("@join");
    [Fact] void should_regenerate_with_escaped_children() => _generated.ShouldContain("@children");
}
