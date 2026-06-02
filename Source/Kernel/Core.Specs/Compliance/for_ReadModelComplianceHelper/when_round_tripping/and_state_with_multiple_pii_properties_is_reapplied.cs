// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.for_ReadModelComplianceHelper.when_round_tripping;

public class and_state_with_multiple_pii_properties_is_reapplied : Specification
{
    const string Identifier = "00000000-0006-0000-0000-000000000006";

    readonly JsonSchema _schema = JsonSchema.FromJson(
        """
        {
          "type": "object",
          "properties": {
            "id": { "type": "string", "format": "guid" },
            "email": {
              "type": "string",
              "compliance": [{ "metadataType": "cae5580e-83d6-44dc-9d7a-a72e8a2f17d7", "details": "" }]
            },
            "personalDetails": {
              "type": ["object", "null"],
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
                }
              },
              "required": ["firstName", "lastName", "displayName"]
            },
            "kcSub": {
              "type": "string",
              "compliance": [{ "metadataType": "cae5580e-83d6-44dc-9d7a-a72e8a2f17d7", "details": "" }]
            },
            "kcSubHash": { "type": "string" },
            "organizationNumber": { "type": "string" },
            "roles": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "role": { "type": "integer" }
                },
                "required": ["role"]
              }
            },
            "isActive": { "type": "boolean", "default": true }
          },
          "required": ["id", "email", "personalDetails", "kcSub", "kcSubHash", "organizationNumber", "roles"]
        }
        """);

    JsonComplianceManager _complianceManager;
    ExpandoObjectConverter _converter;
    ExpandoObject _encryptedInitialState;
    ExpandoObject _releasedState;
    Exception? _exception;

    void Establish()
    {
        var encryption = new Encryption();
        var keyStorage = new InMemoryEncryptionKeyStorage();
        var piiHandler = new PIICompliancePropertyValueHandler(keyStorage, encryption);
        _complianceManager = new(new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(piiHandler));
        _converter = new(new TypeFormats());

        _encryptedInitialState = ReadModelComplianceHelper
            .Apply(_complianceManager, "Ada", "Default", _schema, Identifier, CreateUserState(), _converter)
            .GetAwaiter()
            .GetResult();
    }

    async Task Because()
    {
        try
        {
            _releasedState = await ReadModelComplianceHelper.Release(
                _complianceManager,
                "Ada",
                "Default",
                _schema,
                _encryptedInitialState,
                _converter);

            ((IDictionary<string, object?>)_releasedState)["organizationNumber"] = "123456785";

            _ = await ReadModelComplianceHelper.Apply(
                _complianceManager,
                "Ada",
                "Default",
                _schema,
                Identifier,
                _releasedState,
                _converter);
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    [Fact] void should_not_fail_reapplying_compliance() => _exception.ShouldBeNull();

    [Fact]
    void should_release_existing_pii_values_before_reapplying()
    {
        var state = (IDictionary<string, object?>)_releasedState;
        state["email"].ShouldEqual("client.contact@hiveconsulting.no");
        state["kcSub"].ShouldEqual(Identifier);
    }

    static ExpandoObject CreateUserState()
    {
        dynamic state = new ExpandoObject();
        state.id = Guid.Parse(Identifier);
        state.email = "client.contact@hiveconsulting.no";
        state.personalDetails = null;
        state.kcSub = Identifier;
        state.kcSubHash = "7ab152c581ac0d5230087398a509ffdcf6706cb4fcb8573ba30b5af1d5e153e2";
        state.roles = Array.Empty<object>();
        state.isActive = true;
        return state;
    }
}
