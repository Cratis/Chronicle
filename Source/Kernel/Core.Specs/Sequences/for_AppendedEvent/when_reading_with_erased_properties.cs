// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendedEvent;

public class when_reading_with_erased_properties : Specification
{
    IStorage _storage;
    IEventCompliance _compliance;
    IEventSequenceStorage _sequence;
    Concepts.Events.AppendedEvent _stored;
    AppendedEvent _result;
    AppendedEvent _legacy;
    JsonElement _webhookPayload;
    JsonElement _legacyWebhookPayload;
    JsonElement _apiPayload;
    string _legacyApiJson;
    string _originalApiJson;
    JsonElement _emptyReportingApiPayload;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _compliance = Substitute.For<IEventCompliance>();
        var store = Substitute.For<IEventStoreStorage>();
        var tenant = Substitute.For<IEventStoreNamespaceStorage>();
        _sequence = Substitute.For<IEventSequenceStorage>();
        var cursor = Substitute.For<IEventCursor>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(store);
        store.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(tenant);
        tenant.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(_sequence);
        store.EventTypes.GetFor(Arg.Any<IEnumerable<Concepts.Events.EventType>>()).Returns([]);
        _stored = new(Concepts.Events.EventContext.Empty with { SequenceNumber = 42 }, new ExpandoObject());
        _sequence.GetFromSequenceNumberWithHistory(Arg.Any<Concepts.Events.EventSequenceNumber>(), Arg.Any<Concepts.Events.EventSourceId?>(), Arg.Any<Concepts.Events.EventSourceType?>(), Arg.Any<Concepts.Events.EventStreamType?>(), Arg.Any<Concepts.Events.EventStreamId?>(), Arg.Any<IEnumerable<Concepts.Events.EventType>>()).Returns(cursor);
        cursor.MoveNext().Returns(true, false, true, false);
        cursor.Current.Returns([_stored]);
        _compliance.ReleaseReportingErasure(Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<IDictionary<Concepts.Events.EventType, EventTypeSchema>>()).Returns([_stored with { ErasedProperties = ["contacts[0].email"] }]);
        _compliance.Release(Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<IDictionary<Concepts.Events.EventType, EventTypeSchema>>()).Returns([_stored]);
    }

    async Task Because()
    {
        _result = (await AppendedEvent.FromSequenceNumberWithErasedProperties(_storage, _compliance, JsonSerializerOptions.Default, "store", "tenant", "log", 42, "source", "one,two")).Single();
        _legacy = (await AppendedEvent.FromSequenceNumber(_storage, _compliance, JsonSerializerOptions.Default, "store", "tenant", "log", 42, "source", "one,two")).Single();
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        _webhookPayload = JsonSerializer.SerializeToElement(new EventsToObserve("source", [_stored with { ErasedProperties = ["contacts[0].email"] }]), options);
        _legacyWebhookPayload = JsonSerializer.SerializeToElement(new EventsToObserve("source", [_stored]), options);
        _apiPayload = JsonSerializer.SerializeToElement(_result, options);
        _legacyApiJson = JsonSerializer.Serialize(_legacy, options);
        _originalApiJson = JsonSerializer.Serialize(
            new
            {
                _legacy.Id,
                _legacy.Context,
                _legacy.Content,
                _legacy.OriginalContent,
                _legacy.Revisions,
                _legacy.GenerationalContent,
                _legacy.RevisedGeneration
            },
            options);
        _emptyReportingApiPayload = JsonSerializer.SerializeToElement(_result with { ErasedProperties = [] }, options);
    }

    [Fact] void should_copy_erased_paths_to_the_api() => _result.ErasedProperties.ShouldEqual<IEnumerable<string>>(["contacts[0].email"]);
    [Fact] void should_keep_existing_reads_unannotated() => _legacy.ErasedProperties.ShouldBeEmpty();
    [Fact] void should_preserve_sequence_identity() => _result.Id.ShouldEqual("42");
    [Fact] void should_serialize_paths_as_additive_metadata() => _apiPayload.GetProperty("erasedProperties").EnumerateArray().Select(_ => _.GetString()).ShouldEqual<IEnumerable<string?>>(["contacts[0].email"]);
    [Fact] void should_serialize_metadata_as_exactly_one_json_field() => _apiPayload.EnumerateObject().Select(_ => _.Name).ShouldEqual<IEnumerable<string>>(["id", "context", "content", "originalContent", "revisions", "generationalContent", "revisedGeneration", "erasedProperties"]);
    [Fact] void should_preserve_the_original_json_shape_for_existing_reads() => _legacyApiJson.ShouldEqual(_originalApiJson);
    [Fact] void should_omit_empty_metadata_for_explicit_reporting_reads() => _emptyReportingApiPayload.TryGetProperty("erasedProperties", out _).ShouldBeFalse();
    [Fact] void should_leave_stored_metadata_empty() => _stored.ErasedProperties.ShouldBeEmpty();
    [Fact] void should_not_serialize_transient_metadata_in_webhook_payloads() => _webhookPayload.GetProperty("events")[0].TryGetProperty("erasedProperties", out _).ShouldBeFalse();
    [Fact] void should_not_add_empty_metadata_to_existing_webhook_payloads() => _legacyWebhookPayload.GetProperty("events")[0].TryGetProperty("erasedProperties", out _).ShouldBeFalse();
}
