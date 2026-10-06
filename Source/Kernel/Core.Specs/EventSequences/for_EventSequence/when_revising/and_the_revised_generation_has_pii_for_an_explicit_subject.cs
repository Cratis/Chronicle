// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_the_revised_generation_has_pii_for_an_explicit_subject : given.an_event_sequence_omitting_causation_properties
{
    const string SubjectIdentifier = "person-not-the-event-source";
    const string PersonalValue = "revised personal value";
    const string ProtectedValue = "protected revision";

    EventType _revisedType;
    JsonSchema _schema;
    JsonObject _content;
    ExpandoObject _storedContent;

    void Establish()
    {
        _revisedType = new(_eventType.Id, 2);
        _schema = JsonSchema.FromJson(
            """
            {"type":"object","properties":{"profile":{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}}}
            """);
        _content = new JsonObject { ["profile"] = new JsonObject { ["name"] = PersonalValue } };
        _eventTypesStorage.GetFor(_revisedType.Id, _revisedType.Generation).Returns(
            new EventTypeSchema(_revisedType, EventTypeOwner.Server, EventTypeSource.Code, _schema));
        _eventSequenceStorage.GetEventAt(0UL).Returns(new AppendedEvent(
            EventContext.From(
                EventStore,
                EventStoreNamespace,
                _eventType,
                EventSourceType.Default,
                _eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                0UL,
                CorrelationId.New(),
                subject: SubjectIdentifier),
            new ExpandoObject()));
        _complianceManager.Apply(EventStore, EventStoreNamespace, _schema, SubjectIdentifier, _content).Returns(
            new JsonObject { ["profile"] = new JsonObject { ["name"] = ProtectedValue } });
        var converter = new ExpandoObjectConverter(new TypeFormats());
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), _schema).Returns(
            call => converter.ToExpandoObject(call.ArgAt<JsonObject>(0), _schema));
    }

    async Task Because()
    {
        await _eventSequence.Revise(0UL, _revisedType, _content, CorrelationId.New(), [], Identity.System);
        _storedContent = (ExpandoObject)_eventSequenceStorage.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == nameof(IEventSequenceStorage.Revise))
            .GetArguments()[6]!;
    }

    [Fact] async Task should_apply_the_revised_schema_with_the_original_subject() => await _complianceManager.Received(1).Apply(
        EventStore, EventStoreNamespace, _schema, SubjectIdentifier, _content);
    [Fact] void should_store_protected_revision_content() =>
        ((IDictionary<string, object?>)((IDictionary<string, object?>)_storedContent)["profile"]!)["name"].ShouldEqual(ProtectedValue);
}
