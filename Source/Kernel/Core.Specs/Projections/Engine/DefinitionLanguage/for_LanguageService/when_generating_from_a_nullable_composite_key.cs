// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_generating_from_a_nullable_composite_key : given.a_language_service_with_schemas<given.NullableCompositeOrderReadModel>
{
    const string Declaration = """
        projection Order => NullableCompositeOrderReadModel
          from UserAdded
            key OrderKey
              customerId = userId
              orderNumber = name
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAdded)];

    string _generated;

    void Establish()
    {
        var schema = _readModelDefinition.GetSchemaForLatestGeneration();
        var originalId = JsonNode.Parse(schema.Properties["id"].ToJson());
        schema.Properties["id"] = new JsonSchemaProperty(
            "id",
            new JsonObject
            {
                ["oneOf"] = new JsonArray
                {
                    new JsonObject { ["type"] = "null" },
                    originalId
                }
            },
            schema);
    }

    void Because()
    {
        var compiled = _languageService.Compile(Declaration, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        var eventType = compiled.From.Keys.Single();
        compiled.From[eventType] = compiled.From[eventType] with { Key = "$composite(customerId=userId,orderNumber=name)" };
        _generated = _languageService.Generate(compiled, _readModelDefinition);
        _languageService.Compile(_generated, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(_ => true, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
    }

    [Fact] void should_generate_a_recompilable_nullable_key() => _generated.ShouldContain("key OrderKey");
}
