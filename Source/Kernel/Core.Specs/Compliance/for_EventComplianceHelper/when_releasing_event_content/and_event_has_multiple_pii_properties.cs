// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_EventComplianceHelper.when_releasing_event_content;

public class and_event_has_multiple_pii_properties : Specification
{
    const string Identifier = "00000000-0006-0000-0000-000000000006";

    readonly EventType _eventType = new("PersonalInformationUpdated", EventTypeGeneration.First);
    readonly JsonSchema _schema = JsonSchema.FromJson(
        """
        {
          "type": "object",
          "properties": {
            "firstName": {
              "type": "string",
              "compliance": [{ "metadataType": "cae5580e-83d6-44dc-9d7a-a72e8a2f17d7", "details": "" }]
            },
            "lastName": {
              "type": "string",
              "compliance": [{ "metadataType": "cae5580e-83d6-44dc-9d7a-a72e8a2f17d7", "details": "" }]
            },
            "displayName": {
              "type": "string",
              "compliance": [{ "metadataType": "cae5580e-83d6-44dc-9d7a-a72e8a2f17d7", "details": "" }]
            },
            "bio": {
              "type": "string",
              "compliance": [{ "metadataType": "cae5580e-83d6-44dc-9d7a-a72e8a2f17d7", "details": "" }]
            }
          },
          "required": ["firstName", "lastName", "displayName", "bio"]
        }
        """);

    JsonComplianceManager _complianceManager;
    ExpandoObjectConverter _converter;
    AppendedEvent _released;

    void Establish()
    {
        var encryption = new Encryption();
        var keyStorage = new InMemoryEncryptionKeyStorage();
        var piiHandler = new PIICompliancePropertyValueHandler(keyStorage, encryption);
        _complianceManager = new(new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(piiHandler));
        _converter = new(new TypeFormats());
    }

    async Task Because()
    {
        var encryptedContent = await _complianceManager.Apply(
            "Ada",
            "Default",
            _schema,
            Identifier,
            _converter.ToJsonObject(CreateEventContent(), _schema));

        _released = await EventComplianceHelper.ReleaseEventContent(
            _complianceManager,
            _converter,
            CreateEvent(_converter.ToExpandoObject(encryptedContent, _schema)),
            _schema);
    }

    [Fact]
    void should_release_first_name()
    {
        var content = (IDictionary<string, object?>)_released.Content;
        content["firstName"].ShouldEqual("Client");
    }

    [Fact]
    void should_release_display_name()
    {
        var content = (IDictionary<string, object?>)_released.Content;
        content["displayName"].ShouldEqual("Client Contact");
    }

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

    static ExpandoObject CreateEventContent()
    {
        dynamic content = new ExpandoObject();
        content.firstName = "Client";
        content.lastName = "Contact";
        content.displayName = "Client Contact";
        content.bio = string.Empty;
        return content;
    }
}
