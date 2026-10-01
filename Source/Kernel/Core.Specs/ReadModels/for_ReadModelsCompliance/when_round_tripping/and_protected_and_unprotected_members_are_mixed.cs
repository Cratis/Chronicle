// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping;

public class and_protected_and_unprotected_members_are_mixed : Specification
{
    const string ActiveSubject = "active-person";
    const string ErasedSubject = "erased-person";
    JsonSchema _schema;
    InMemoryEncryptionKeyStorage _keys;
    ReadModelsCompliance _compliance;
    ExpandoObject _stored;
    ExpandoObject _released;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "activeName": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
                "activeNickname": { "type": ["string", "null"], "compliance": [{ "metadataType": "PII", "details": "" }] },
                "erasedName": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
                "erasedAge": { "type": ["integer", "null"], "compliance": [{ "metadataType": "PII", "details": "" }] },
                "status": { "type": "string" },
                "optionalStatus": { "type": ["string", "null"] },
                "count": { "type": "integer" },
                "optionalCount": { "type": ["integer", "null"] },
                "details": {
                  "type": "object",
                  "properties": {
                    "privateAge": { "type": ["integer", "null"], "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "optionalStatus": { "type": ["string", "null"] },
                    "count": { "type": "integer" }
                  }
                }
              }
            }
            """);
        _keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(_keys, encryption), _keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _compliance = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));
        await _keys.RecordErasureFor("store", "Default", ErasedSubject);
    }

    async Task Because()
    {
        dynamic instance = new ExpandoObject();
        instance.activeName = "Ada Lovelace";
        instance.activeNickname = string.Empty;
        instance.erasedName = "Grace Hopper";
        instance.erasedAge = 85;
        instance.status = "updated";
        instance.optionalStatus = null;
        instance.count = 42;
        instance.optionalCount = null;
        dynamic details = new ExpandoObject();
        details.privateAge = 91;
        details.optionalStatus = null;
        details.count = 73;
        instance.details = details;
        instance.__subjects = ReadModelSubjects.ToExpandoObject(new Dictionary<string, string>
        {
            ["erasedName"] = ErasedSubject,
            ["erasedAge"] = ErasedSubject,
            ["optionalCount"] = ErasedSubject,
            ["details"] = ErasedSubject
        });
        _stored = await _compliance.Apply("store", "Default", _schema, ActiveSubject, instance);
        _released = await _compliance.Release("store", "Default", _schema, _stored);
    }

    [Fact] void should_encrypt_the_active_name() => ProtectedValueCodec.TryDecodeCipherText(new Encryption(), Value(_stored, "activeName")!.ToString()!, out _).ShouldBeTrue();
    [Fact] void should_release_the_active_name() => Value(_released, "activeName").ShouldEqual("Ada Lovelace");
    [Fact] void should_release_the_active_empty_nickname() => Value(_released, "activeNickname").ShouldEqual(string.Empty);
    [Fact] void should_placeholder_only_the_erased_name() => Value(_stored, "erasedName").ShouldEqual(string.Empty);
    [Fact] void should_not_restore_the_erased_nullable_age() => Value(_stored, "erasedAge").ShouldBeNull();
    [Fact] void should_preserve_the_non_personal_status() => Value(_stored, "status").ShouldEqual("updated");
    [Fact] void should_preserve_the_non_personal_count() => Value(_stored, "count").ShouldEqual(42);
    [Fact] void should_keep_the_active_subjects_explicit_non_personal_null() => Contains(_stored, "optionalStatus").ShouldBeTrue();
    [Fact] void should_keep_the_active_subjects_non_personal_null_value() => Value(_stored, "optionalStatus").ShouldBeNull();
    [Fact] void should_keep_the_erased_subjects_explicit_non_personal_null() => Contains(_stored, "optionalCount").ShouldBeTrue();
    [Fact] void should_keep_the_erased_subjects_non_personal_null_value() => Value(_stored, "optionalCount").ShouldBeNull();
    [Fact] void should_keep_the_nested_explicit_non_personal_null() => Contains((ExpandoObject)Value(_stored, "details")!, "optionalStatus").ShouldBeTrue();
    [Fact] void should_preserve_the_nested_non_personal_count() => Value((ExpandoObject)Value(_stored, "details")!, "count").ShouldEqual(73);
    [Fact] void should_not_restore_nested_erased_personal_data() => Value((ExpandoObject)Value(_stored, "details")!, "privateAge").ShouldBeNull();
    [Fact] async Task should_not_create_a_key_for_the_erased_subject() => (await _keys.HasFor("store", "Default", ErasedSubject)).ShouldBeFalse();

    static bool Contains(ExpandoObject instance, string property) => ((IDictionary<string, object?>)instance).ContainsKey(property);
    static object? Value(ExpandoObject instance, string property) => ((IDictionary<string, object?>)instance).TryGetValue(property, out var value) ? value : null;
}
