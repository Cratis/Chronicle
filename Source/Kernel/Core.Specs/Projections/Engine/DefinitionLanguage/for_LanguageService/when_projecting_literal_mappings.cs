// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_projecting_literal_mappings : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Declaration = """
        projection User => UserReadModel
          from UserCreated
            key userId
            status = "draft"
            isActive = true
            age = 1
            score = 4.5
            add version by 2
            subtract rating by 1
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserCreated)];

    ExpandoObject _target;

    void Because()
    {
        var definition = CompileGenerateAndRecompile(Declaration).Definition;
        var mappings = definition.From[(EventType)"UserCreated"].Properties.ToDictionary();
        mappings[new PropertyPath("largeNumber")] = "9007199254740993";
        var typeFormats = new TypeFormats();
        var valueResolvers = new EventValueProviderExpressionResolvers(typeFormats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var resolvers = new ReadModelPropertyExpressionResolvers(valueResolvers, typeFormats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance);
        _target = new ExpandoObject();
        var properties = (IDictionary<string, object?>)_target;
        properties["version"] = 5;
        properties["rating"] = 5d;

        var @event = new AppendedEvent(
            new(
                new EventType("02405794-91e7-4e4f-8ad1-f043070ca297", 1),
                EventSourceType.Default,
                "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
                EventStreamType.All,
                EventStreamId.Default,
                0,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            new ExpandoObject());

        foreach (var (property, expression) in mappings)
        {
            var type = property.Path switch
            {
                "status" => JsonObjectType.String,
                "isActive" => JsonObjectType.Boolean,
                "rating" or "score" => JsonObjectType.Number,
                _ => JsonObjectType.Integer
            };
            var mapper = resolvers.Resolve(property, new JsonSchemaProperty { Type = type, Format = property.Path == "largeNumber" ? "int64" : null }, expression);
            mapper(@event, _target, ArrayIndexers.NoIndexers);
        }
    }

    [Fact] void should_set_the_string_without_quotes() => ((IDictionary<string, object?>)_target)["status"].ShouldEqual("draft");
    [Fact] void should_set_the_boolean() => ((IDictionary<string, object?>)_target)["isActive"].ShouldEqual(true);
    [Fact] void should_set_the_integer() => ((IDictionary<string, object?>)_target)["age"].ShouldEqual(1);
    [Fact] void should_preserve_the_large_integer() => ((IDictionary<string, object?>)_target)["largeNumber"].ShouldEqual(9007199254740993L);
    [Fact] void should_set_the_fractional_number() => ((IDictionary<string, object?>)_target)["score"].ShouldEqual(4.5d);
    [Fact] void should_add_the_literal_operand() => ((IDictionary<string, object?>)_target)["version"].ShouldEqual(7);
    [Fact] void should_subtract_the_literal_operand() => ((IDictionary<string, object?>)_target)["rating"].ShouldEqual(4d);
}
