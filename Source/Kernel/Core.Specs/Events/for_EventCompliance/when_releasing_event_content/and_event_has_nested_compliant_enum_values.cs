// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Events.for_EventCompliance.when_releasing_event_content;

public class and_event_has_nested_compliant_enum_values : Specification
{
    const string Identifier = "nested-enum-subject";

    readonly EventType _eventType = new("EnumValuesRecorded", EventTypeGeneration.First);
    readonly JsonSchema _schema = JsonSchema.FromJson(
        """
        {
          "type": "object",
          "properties": {
            "profile": {
              "type": "object",
              "properties": {
                "zeroStatus": {
                  "type": "integer",
                  "enum": [0, 1],
                  "x-enumNames": ["Unknown", "Verified"],
                  "compliance": [{ "metadataType": "PII", "details": "" }]
                },
                "nullableStatus": {
                  "type": ["integer", "null"],
                  "enum": [0, 1],
                  "x-enumNames": ["Unknown", "Verified"],
                  "compliance": [{ "metadataType": "PII", "details": "" }]
                },
                "nullableValuedStatus": {
                  "type": ["integer", "null"],
                  "enum": [0, 1],
                  "x-enumNames": ["Unknown", "Verified"],
                  "compliance": [{ "metadataType": "PII", "details": "" }]
                },
                "nested": {
                  "type": "object",
                  "properties": {
                    "nonzeroStatus": {
                      "type": "integer",
                      "enum": [0, 1],
                      "x-enumNames": ["Unknown", "Verified"],
                      "compliance": [{ "metadataType": "PII", "details": "" }]
                    }
                  }
                }
              }
            }
          }
        }
        """);

    JsonObject _encrypted;
    AppendedEvent _released;
    AppendedEvent _shredded;

    async Task Because()
    {
        var keyStorage = new InMemoryEncryptionKeyStorage();
        var manager = new JsonComplianceManager(
            new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(
                new PIICompliancePropertyValueHandler(keyStorage, new Encryption())),
            NullLogger<JsonComplianceManager>.Instance);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var plaintext = new JsonObject
        {
            ["profile"] = new JsonObject
            {
                ["zeroStatus"] = 0,
                ["nullableStatus"] = null,
                ["nullableValuedStatus"] = 1,
                ["nested"] = new JsonObject { ["nonzeroStatus"] = 1 }
            }
        };

        _encrypted = await manager.Apply("Ada", "Default", _schema, Identifier, plaintext);
        var compliance = new EventCompliance(manager, converter);
        var storedEvent = CreateEvent(converter.ToExpandoObject(_encrypted, _schema));
        _released = await compliance.Release(storedEvent, _schema);
        await keyStorage.DeleteFor("Ada", "Default", Identifier);
        _shredded = await compliance.Release(storedEvent, _schema);
    }

    [Fact] void should_encrypt_the_zero_enum_at_rest() => _encrypted["profile"]!["zeroStatus"]!.GetValue<string>().ShouldNotEqual("0");
    [Fact] void should_encrypt_the_deeply_nested_nonzero_enum_at_rest() => _encrypted["profile"]!["nested"]!["nonzeroStatus"]!.GetValue<string>().ShouldNotEqual("1");
    [Fact] void should_leave_the_nullable_null_value_null() => (_encrypted["profile"]!["nullableStatus"] is null).ShouldBeTrue();
    [Fact] void should_encrypt_the_nullable_valued_enum_at_rest() => _encrypted["profile"]!["nullableValuedStatus"]!.GetValue<string>().ShouldNotEqual("1");
    [Fact] void should_release_the_zero_enum() => Profile["zeroStatus"].ShouldEqual(0);
    [Fact] void should_release_the_deeply_nested_nonzero_enum() => Nested["nonzeroStatus"].ShouldEqual(1);
    [Fact] void should_release_the_nullable_valued_enum() => Profile["nullableValuedStatus"].ShouldEqual(1);
    [Fact] void should_keep_the_nullable_null_value_absent() => Profile.ContainsKey("nullableStatus").ShouldBeFalse();
    [Fact] void should_keep_the_event_readable_after_crypto_shredding() =>
        ((IDictionary<string, object?>)((IDictionary<string, object?>)_shredded.Content)["profile"]!)["zeroStatus"].ShouldEqual(string.Empty);

    IDictionary<string, object?> Content => _released.Content;
    IDictionary<string, object?> Profile => (IDictionary<string, object?>)Content["profile"]!;
    IDictionary<string, object?> Nested => (IDictionary<string, object?>)Profile["nested"]!;

    AppendedEvent CreateEvent(ExpandoObject content) =>
        new(
            EventContext.From(
                "Ada",
                "Default",
                _eventType,
                EventSourceType.Default,
                Identifier,
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.NotSet,
                subject: Identifier),
            content);
}
