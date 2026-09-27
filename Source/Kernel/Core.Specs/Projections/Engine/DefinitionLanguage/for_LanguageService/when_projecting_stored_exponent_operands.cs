// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_projecting_stored_exponent_operands : given.a_language_service_with_schemas<given.UserReadModel>
{
    IDictionary<string, object?> _target;
    string _generated;

    void Because()
    {
        var definition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            _projectionId,
            _readModelDefinition.Identifier,
            true,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>
            {
                [(EventType)"UserCreated"] = new FromDefinition(
                    new Dictionary<PropertyPath, string>
                    {
                        [new PropertyPath("score")] = $"{WellKnownExpressions.Add}(1e-3)",
                        [new PropertyPath("rating")] = $"{WellKnownExpressions.Subtract}(-2.5e+0)"
                    },
                    PropertyExpression.NotSet,
                    null)
            },
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());
        _generated = _languageService.Generate(definition, _readModelDefinition);

        var formats = new TypeFormats();
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var resolvers = new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance);
        var target = new ExpandoObject();
        _target = target;
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

        foreach (var (property, expression) in definition.From[(EventType)"UserCreated"].Properties)
        {
            var mapper = resolvers.Resolve(property, new JsonSchemaProperty { Type = JsonObjectType.Number }, expression);
            mapper(@event, target, ArrayIndexers.NoIndexers);
        }
    }

    [Fact] void should_add_the_exponent() => _target["score"].ShouldEqual(5.001d);
    [Fact] void should_subtract_the_signed_exponent() => _target["rating"].ShouldEqual(7.5d);
    [Fact] void should_generate_the_add_operand() => _generated.ShouldContain("add score by 1e-3");
    [Fact] void should_generate_the_subtract_operand() => _generated.ShouldContain("subtract rating by -2.5e+0");
}
