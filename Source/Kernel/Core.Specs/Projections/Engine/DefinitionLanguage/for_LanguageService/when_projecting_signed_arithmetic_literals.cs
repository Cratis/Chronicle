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

public class when_projecting_signed_arithmetic_literals : given.a_language_service_with_schemas<given.UserReadModel>
{
    const string Declaration = """
        projection User => UserReadModel
          from UserCreated
            key userId
            add age by -1
            add score by 1.5
            subtract rating by -2.5
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserCreated)];

    IDictionary<string, object?> _target;

    void Because()
    {
        var mappings = CompileGenerateAndRecompile(Declaration).Definition.From[(EventType)"UserCreated"].Properties;
        var formats = new TypeFormats();
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var resolvers = new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance);
        var target = new ExpandoObject();
        _target = target;
        _target["age"] = 5;
        _target["score"] = 5d;
        _target["rating"] = 5d;

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
            var isFloatingPoint = property.Path == "score" || property.Path == "rating";
            var mapper = resolvers.Resolve(
                property,
                new JsonSchemaProperty
                {
                    Type = isFloatingPoint ? JsonObjectType.Number : JsonObjectType.Integer
                },
                expression);
            mapper(@event, target, ArrayIndexers.NoIndexers);
        }
    }

    [Fact] void should_add_a_negative_integer() => _target["age"].ShouldEqual(4);
    [Fact] void should_add_a_decimal() => _target["score"].ShouldEqual(6.5d);
    [Fact] void should_subtract_a_negative_decimal() => _target["rating"].ShouldEqual(7.5d);
}
