// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_the_appended_copy_is_released_with_inherited_protection : given.an_event_sequence_with_inherited_protection
{
    const string Marker = "central-inherited-pii-marker";
    AppendedEvent _appended;
    EventCompliance _compliance;
    JsonSchema _own;
    AppendedEvent _single;
    AppendedEvent[] _batch;

    async Task Establish()
    {
        await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, Plaintext(Marker), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None, subject: new Subject(SubjectIdentifier));
        var stored = StoredEvents().Single();
        _own = _definition.Generations.Single(_ => _.Generation == _eventType.Generation).Schema;
        _appended = new(stored.Context with { Subject = SubjectIdentifier }, _converter.ToExpandoObject(JsonNode.Parse(stored.GenerationalContent![1])!.AsObject(), _own));
        _compliance = new(_metadataManager, _converter, _schemaCache);
    }

    async Task Because()
    {
        _single = await _compliance.Release(_appended, _own);
        _batch = await _compliance.Release([_appended], new Dictionary<EventType, EventTypeSchema> { [_eventType] = new(_eventType, EventTypeOwner.Client, EventTypeSource.Code, _own) });
    }

    [Fact] void should_release_the_single_event() => ReleasedName(_single).ShouldEqual(Marker);
    [Fact] void should_not_skip_the_batch_because_the_own_schema_has_no_metadata() => ReleasedName(_batch.Single()).ShouldEqual(Marker);
    [Fact] void should_not_mutate_the_protected_input() => ReleasedName(_appended).ShouldNotEqual(Marker);

    [Fact]
    async Task should_not_release_the_appended_marker_after_erasure()
    {
        await _keys.DeleteFor(EventStore, EventStoreNamespace, SubjectIdentifier);
        ReleasedName(await _compliance.Release(_appended, _own)).ShouldBeEmpty();
    }

    [Fact]
    void should_classify_inherited_personal_properties_for_constraints()
    {
        var call = _constraintValidation.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == nameof(IConstraintValidation.Establish));
        ((IReadOnlyCollection<string>)call.GetArguments()[8]!).ShouldContain("name");
        var plaintext = (ExpandoObject)call.GetArguments()[2]!;
        _converter.ToJsonObject(plaintext, _own)["name"]!.GetValue<string>().ShouldEqual(Marker);
    }

    [Fact]
    async Task should_release_and_classify_the_stored_constraint_input()
    {
        var content = new StoredConstraintEventContent(EventStore, EventStoreNamespace, _eventTypesStorage, _metadataManager, _converter, _schemaCache);
        var released = await content.Release(_appended);
        _converter.ToJsonObject(released, _own)["name"]!.GetValue<string>().ShouldEqual(Marker);
        (await content.PersonalPropertiesOf(_appended)).ShouldContain("name");
    }

    string ReleasedName(AppendedEvent @event) => _converter.ToJsonObject(@event.Content, _own)["name"]!.GetValue<string>();
}
