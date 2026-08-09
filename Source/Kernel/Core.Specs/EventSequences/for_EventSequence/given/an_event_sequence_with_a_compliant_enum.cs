// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_a_compliant_enum : an_event_sequence
{
    protected JsonSchema _compliantEnumSchema;
    protected JsonComplianceManager _realComplianceManager;
    protected ExpandoObjectConverter _realConverter;

    void Establish()
    {
        _compliantEnumSchema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "status": {
                        "type": "integer",
                        "enum": [0, 1],
                        "x-enumNames": ["Unknown", "Verified"],
                        "compliance": [{ "metadataType": "PII", "details": "" }]
                    }
                },
                "required": ["status"]
            }
            """);

        _eventTypesStorage.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(new EventTypeSchema(_eventType, EventTypeOwner.Server, EventTypeSource.Code, _compliantEnumSchema));

        _realComplianceManager = new(
            new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(
                new PIICompliancePropertyValueHandler(new InMemoryEncryptionKeyStorage(), new Encryption())),
            NullLogger<JsonComplianceManager>.Instance);
        _complianceManager.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), _compliantEnumSchema, Arg.Any<string>(), Arg.Any<JsonObject>())
            .Returns(callInfo => _realComplianceManager.Apply(
                callInfo.ArgAt<EventStoreName>(0),
                callInfo.ArgAt<EventStoreNamespaceName>(1),
                _compliantEnumSchema,
                callInfo.ArgAt<string>(3),
                callInfo.ArgAt<JsonObject>(4)));

        _realConverter = new(new TypeFormats());
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), _compliantEnumSchema)
            .Returns(callInfo => _realConverter.ToExpandoObject(callInfo.ArgAt<JsonObject>(0), _compliantEnumSchema));

        _eventTypeMigrations.MigrateToAllGenerations(Arg.Any<EventStoreName>(), Arg.Any<EventType>(), Arg.Any<JsonObject>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo => new Dictionary<EventTypeGeneration, ExpandoObject>
            {
                [EventTypeGeneration.First] = callInfo.ArgAt<ExpandoObject>(3)
            });
    }

    protected JsonObject ValidContent() => new() { ["status"] = 0 };
}
