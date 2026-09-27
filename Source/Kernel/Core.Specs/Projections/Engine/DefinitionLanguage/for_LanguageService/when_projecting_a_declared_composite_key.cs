// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_projecting_a_declared_composite_key : given.a_language_service_with_schemas<given.KeywordKeyReadModel>
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

    Key _key;

    async Task Because()
    {
        var definition = CompileGenerateAndRecompile(Declaration).Definition;
        var projection = Substitute.For<IProjection>();
        projection.Identifier.Returns(definition.Identifier);
        projection.ReadModel.Returns(_readModelDefinition);
        var typeFormats = new TypeFormats();
        var eventResolvers = new EventValueProviderExpressionResolvers(typeFormats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var keyResolvers = new KeyExpressionResolvers(eventResolvers, new KeyResolvers(NullLogger<KeyResolvers>.Instance), NullLogger<KeyExpressionResolvers>.Instance);
        var expression = definition.From[(EventType)"UserRegisteredWithKeywordValues"].Key.Value;
        var value = new ExpandoObject();
        var properties = (IDictionary<string, object?>)value;
        properties["from"] = "source";
        properties["projection"] = "projection";
        properties["key"] = "key";
        properties["join"] = "join";
        properties["children"] = "children";
        var @event = new AppendedEvent(
            new(
                (EventType)"UserRegisteredWithKeywordValues",
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
            value);
        var resolved = await keyResolvers.Resolve(projection, expression, PropertyPath.NotSet)(null!, null!, @event);
        _key = ((ResolvedKey)resolved).Key;
    }

    [Fact] void should_project_the_first_component() => ((IDictionary<string, object>)_key.Value)["from"].ShouldEqual("source");
    [Fact] void should_project_the_last_component() => ((IDictionary<string, object>)_key.Value)["children"].ShouldEqual("children");
}
