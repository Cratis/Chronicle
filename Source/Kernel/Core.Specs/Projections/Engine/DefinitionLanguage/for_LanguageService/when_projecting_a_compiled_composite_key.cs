// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_LanguageService;

public class when_projecting_a_compiled_composite_key : given.a_language_service_with_schemas<given.CompositeOrderReadModel>
{
    const string Declaration = """
        projection Order => CompositeOrderReadModel
          from UserAdded
            key OrderKey
              customerId = userId
              orderNumber = name
            name = name
        """;

    protected override IEnumerable<Type> EventTypes => [typeof(given.UserAdded)];

    IDictionary<string, object?> _key;
    object? _projectedName;

    async Task Because()
    {
        var definition = _languageService.Compile(Declaration, Concepts.Projections.ProjectionOwner.Client, [_readModelDefinition], _eventTypeSchemas)
            .Match(value => value, errors => throw new InvalidOperationException(string.Join(", ", errors.Errors)));
        var storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore("store").Returns(eventStore);
        eventStore.GetNamespace("namespace").Returns(@namespace);
        @namespace.GetEventSequence(EventSequenceId.Log).Returns(Substitute.For<IEventSequenceStorage>());
        var formats = new TypeFormats();
        var keys = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keys, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(formats),
            keys,
            storage,
            NullLogger<ProjectionFactory>.Instance);
        using var projection = (Projection)await factory.Create("store", "namespace", definition, _readModelDefinition, _eventTypeSchemas);
        var eventType = definition.From.Keys.Single();
        var payload = new ExpandoObject();
        ((IDictionary<string, object?>)payload)["userId"] = "customer-1";
        ((IDictionary<string, object?>)payload)["name"] = "order-2";
        var @event = new AppendedEvent(
            new(
                eventType,
                EventSourceType.Default,
                "source",
                EventStreamType.All,
                EventStreamId.Default,
                0,
                DateTimeOffset.UtcNow,
                "event",
                "causation",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            payload);
        var resolved = await projection.GetKeyResolverFor(eventType)(Substitute.For<IEventSequenceStorage>(), Substitute.For<ISink>(), @event);
        var key = ((ResolvedKey)resolved).Key;
        _key = (IDictionary<string, object?>)key.Value;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(Substitute.For<IObjectComparer>(), @event, new ExpandoObject());
        projection.OnNext(new ProjectionEventContext(key, @event, changeset, ProjectionOperationType.From, false));
        _projectedName = ((IDictionary<string, object?>)changeset.CurrentState)["name"];
    }

    [Fact] void should_project_the_event() => _projectedName.ShouldEqual("order-2");
    [Fact] void should_resolve_first_key_part() => _key["customerId"].ShouldEqual("customer-1");
    [Fact] void should_resolve_second_key_part() => _key["orderNumber"].ShouldEqual("order-2");
}
