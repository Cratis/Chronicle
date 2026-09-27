// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_compiling_and_mapping_caused_by : given.a_language_service_with_schemas<given.ActivityReadModel>
{
    const string Declaration = """
        projection Activity => ActivityReadModel
          from ActivityLogged
            key $eventSourceId
            createdBySubject = $causedBy.subject
            createdByName = $causedBy.name
            createdByUser = $causedBy.userName
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.ActivityLogged)];

    ExpandoObject _target;
    Identity _identity;

    void Because()
    {
        var definition = _languageService.Compile(Declaration, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(projection => projection, errors => throw new InvalidOperationException($"Compilation failed: {string.Join(", ", errors.Errors)}"));
        var mappings = definition.From[(EventType)"ActivityLogged"].Properties;
        var resolvers = new EventValueProviderExpressionResolvers(new TypeFormats(), NullLogger<EventValueProviderExpressionResolvers>.Instance);
        _identity = new Identity("subject-1", "Test User", "test-user");
        var @event = new AppendedEvent(
            new(
                (EventType)"ActivityLogged",
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
                _identity,
                [],
                EventHash.NotSet),
            new ExpandoObject());
        _target = new ExpandoObject();
        foreach (var (path, expression) in mappings)
        {
            var schema = _readModelDefinition.GetSchemaForLatestGeneration().GetSchemaPropertyForPropertyPath(path);
            var mapper = PropertyMappers.FromEventValueProvider(path, resolvers.Resolve(schema!, expression));
            mapper(@event, _target, ArrayIndexers.NoIndexers);
        }
    }

    [Fact] void should_map_subject() => ((IDictionary<string, object?>)_target)["createdBySubject"].ShouldEqual(_identity.Subject);
    [Fact] void should_map_name() => ((IDictionary<string, object?>)_target)["createdByName"].ShouldEqual(_identity.Name);
    [Fact] void should_map_user_name() => ((IDictionary<string, object?>)_target)["createdByUser"].ShouldEqual(_identity.UserName);
}
