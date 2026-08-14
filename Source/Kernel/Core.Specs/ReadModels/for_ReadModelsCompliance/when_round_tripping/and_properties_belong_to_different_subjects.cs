// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping;

public class and_properties_belong_to_different_subjects : Specification
{
    const string DefaultSubject = "person-1";
    const string OtherSubject = "person-2";
    const string DefaultName = "Ada Lovelace";
    const string OtherName = "Grace Hopper";

    readonly JsonSchema _schema = JsonSchema.FromJson(
        """
        {
          "type": "object",
          "properties": {
            "defaultName": {
              "type": "string",
              "compliance": [{ "metadataType": "PII", "details": "" }]
            },
            "otherName": {
              "type": "string",
              "compliance": [{ "metadataType": "PII", "details": "" }]
            }
          }
        }
        """);

    InMemoryEncryptionKeyStorage _keyStorage;
    ReadModelsCompliance _compliance;
    ExpandoObject _encrypted;
    ExpandoObject _released;
    ExpandoObject _releasedAfterErasure;

    void Establish()
    {
        _keyStorage = new InMemoryEncryptionKeyStorage();
        var manager = new JsonComplianceManager(
            new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(
                new PIICompliancePropertyValueHandler(_keyStorage, new Encryption())),
            NullLogger<JsonComplianceManager>.Instance);
        _compliance = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));
    }

    async Task Because()
    {
        dynamic instance = new ExpandoObject();
        instance.defaultName = DefaultName;
        instance.otherName = OtherName;
        instance.__subjects = ReadModelSubjects.ToExpandoObject(new Dictionary<string, string>
        {
            ["otherName"] = OtherSubject
        });

        _encrypted = await _compliance.Apply("test-store", "test-namespace", _schema, DefaultSubject, instance);
        _released = await _compliance.Release("test-store", "test-namespace", _schema, _encrypted);

        await _keyStorage.DeleteFor("test-store", "test-namespace", OtherSubject);
        _releasedAfterErasure = await _compliance.Release("test-store", "test-namespace", _schema, _encrypted);
    }

    [Fact] void should_encrypt_the_default_subjects_property() => Value(_encrypted, "defaultName").ShouldNotEqual(DefaultName);
    [Fact] void should_encrypt_the_other_subjects_property() => Value(_encrypted, "otherName").ShouldNotEqual(OtherName);
    [Fact] void should_release_the_default_subjects_property() => Value(_released, "defaultName").ShouldEqual(DefaultName);
    [Fact] void should_release_the_other_subjects_property() => Value(_released, "otherName").ShouldEqual(OtherName);
    [Fact] void should_keep_the_default_subjects_property_after_erasing_the_other_subject() => Value(_releasedAfterErasure, "defaultName").ShouldEqual(DefaultName);
    [Fact] void should_erase_only_the_other_subjects_property() => Value(_releasedAfterErasure, "otherName").ShouldEqual(string.Empty);

    static string Value(ExpandoObject instance, string property) =>
        ((IDictionary<string, object?>)instance)[property]!.ToString()!;
}
