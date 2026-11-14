// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Events.for_EventCompliance.when_releasing;

public class and_a_date_time_value_with_an_offset_is_beside_a_protected_value : Specification
{
    const string Identifier = "person";
    const string Name = "personal name";
    const string WithOffset = "2025-01-02T03:04:05.1234567+05:30";

    JsonSchema _schema;
    ExpandoObjectConverter _converter;
    JsonSchemaMetadataManager _manager;
    EventCompliance _compliance;
    AppendedEvent _event;
    AppendedEvent _released;
    Exception _exception;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"PII","details":""}]},"moment":{"type":"string","format":"date-time"}}}
            """);
        _converter = new(new TypeFormats());
        var keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(keys, encryption);
        _manager = new(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(provisioner, keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        var cache = Substitute.For<IEventTypeSchemaCache>();
        cache.GetProtectionSchemaFor(Arg.Any<Concepts.EventStoreName>(), Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>(), Arg.Any<JsonSchema>())
            .Returns(new ValueTask<JsonSchema>(_schema));
        _compliance = new(_manager, _converter, cache);

        var content = _converter.ToExpandoObject(Encoding.UTF8.GetBytes($$"""{"name":"{{Name}}","moment":"{{WithOffset}}"}"""), _schema);
        var applied = await _manager.Apply("store", "namespace", _schema, Identifier, _converter.ToJsonObject(content, _schema));
        _event = new(
            EventContext.From("store", "namespace", new EventType("event", 1), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0UL, CorrelationId.New(), subject: Identifier),
            _converter.ToExpandoObject(applied, _schema));
    }

    async Task Because() => _exception = await Catch.Exception(async () => _released = await _compliance.Release(_event, _schema));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_release_the_protected_value() => ((IDictionary<string, object?>)_released.Content)["name"].ShouldEqual(Name);
    [Fact] void should_keep_the_date_time_value_with_its_own_offset() => ((IDictionary<string, object?>)_released.Content)["moment"].ShouldEqual(DateTimeOffset.Parse(WithOffset));
    [Fact] void should_keep_the_offset_of_the_date_time_value() => ((DateTimeOffset)((IDictionary<string, object?>)_released.Content)["moment"]!).Offset.ShouldEqual(TimeSpan.FromMinutes(330));
}
