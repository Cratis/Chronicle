// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_generating_a_legacy_fluent_composite_key : given.a_language_service_with_schemas<given.KeywordKeyReadModel>
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

    ProjectionDefinition _result;
    string _generated;

    void Because()
    {
        var definition = CompileGenerateAndRecompile(Declaration).Definition;
        var eventType = (EventType)"UserRegisteredWithKeywordValues";
        definition.From[eventType] = definition.From[eventType] with
        {
            Key = "$composite(from=from,projection=projection,key=key,join=join,children=children)"
        };
        _generated = _languageService.Generate(definition, _readModelDefinition);
        _result = _languageService.Compile(_generated, ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(compiled => compiled, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
    }

    [Fact] void should_infer_the_key_type_from_the_read_model() => _generated.ShouldContain("key KeywordKey");
    [Fact] void should_keep_all_mappings_on_recompilation() => _result.From[(EventType)"UserRegisteredWithKeywordValues"].Key.Value.ShouldEqual("$composite(KeywordKey, from=from, projection=projection, key=key, join=join, children=children)");
}
