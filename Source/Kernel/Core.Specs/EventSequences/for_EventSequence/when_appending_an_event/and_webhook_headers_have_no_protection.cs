// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_webhook_headers_have_no_protection : given.an_event_sequence
{
    JsonSchema _schema;
    AppendResult _result;
    ExpandoObject _stored;

    void Establish()
    {
        _schema = JsonSchema.FromType<WebhookAddedLike>();
        _eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(new EventTypeSchema(_eventType, EventTypeOwner.Server, EventTypeSource.Code, _schema));
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(), NullLogger<JsonSchemaMetadataManager>.Instance);
        _complianceManager.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), _schema, Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(call => manager.Apply(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), _schema, call.ArgAt<string>(3), call.ArgAt<JsonObject>(4)));
        var converter = new ExpandoObjectConverter(new TypeFormats());
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), _schema)
            .Returns(call => converter.ToExpandoObject(call.ArgAt<JsonObject>(0), _schema));
        _eventTypeMigrations.MigrateToAllGenerations(Arg.Any<EventStoreName>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>())
            .Returns(call =>
            {
                _stored = call.ArgAt<ExpandoObject>(3);
                return new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = _stored };
            });
    }

    async Task Because() => _result = await _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        JsonNode.Parse("""{"targetUrl":"https://example.com/webhook","targetHeaders":{"X-Correlation-Id":"webhook-test"}}""")!.AsObject(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);

    [Fact] void should_append_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_dictionary_headers() => ((IDictionary<string, object>)((IDictionary<string, object?>)_stored)["targetHeaders"]!)["X-Correlation-Id"].ShouldEqual("webhook-test");
    [Fact] void should_exercise_a_generated_dictionary_schema() => _schema.Properties["targetHeaders"].AdditionalPropertiesSchema.ShouldNotBeNull();

    public record WebhookAddedLike(string TargetUrl, IReadOnlyDictionary<string, string> TargetHeaders);
}
