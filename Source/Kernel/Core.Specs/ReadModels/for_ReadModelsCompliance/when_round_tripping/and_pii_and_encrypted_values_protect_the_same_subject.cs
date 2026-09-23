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

/// <summary>
/// The headline guarantee this feature exists to deliver: a subject that owns both a [PII] value and an
/// [Encrypted] value has the two protected under genuinely different keys, end to end through the real
/// JsonSchemaMetadataManager walk and a real InMemoryEncryptionKeyStorage - not merely asserted at the identifier
/// level (see for_EncryptedValueKeyIdentifiers). Erasing the subject's PII key must not touch the [Encrypted]
/// key at all.
/// </summary>
public class and_pii_and_encrypted_values_protect_the_same_subject : Specification
{
    const string Subject = "customer-42";
    const string Email = "ada@example.com";
    const string ApiKey = "sk_live_super_secret_partner_key";

    readonly JsonSchema _schema = JsonSchema.FromJson(
        """
        {
          "type": "object",
          "properties": {
            "email": {
              "type": "string",
              "compliance": [{ "metadataType": "PII", "details": "" }]
            },
            "apiKey": {
              "type": "string",
              "security": [{ "metadataType": "EncryptedSubject", "details": "" }]
            }
          }
        }
        """);

    InMemoryEncryptionKeyStorage _keyStorage;
    ReadModelsCompliance _compliance;
    ExpandoObject _encrypted;
    ExpandoObject _released;
    ExpandoObject _releasedAfterPIIErasure;

    void Establish()
    {
        _keyStorage = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var provisioner = new ManagedEncryptionKeyProvisioner(_keyStorage, encryption);
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(
                new PIICompliancePropertyValueHandler(provisioner, _keyStorage, encryption),
                new EncryptedSubjectValueHandler(provisioner, _keyStorage, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        _compliance = new ReadModelsCompliance(manager, new ExpandoObjectConverter(new TypeFormats()));
    }

    async Task Because()
    {
        dynamic instance = new ExpandoObject();
        instance.email = Email;
        instance.apiKey = ApiKey;

        _encrypted = await _compliance.Apply("test-store", "test-namespace", _schema, Subject, instance);
        _released = await _compliance.Release("test-store", "test-namespace", _schema, _encrypted);

        // Right-to-erasure destroys the PII key by the bare subject identifier - exactly what PIIManager does.
        // The [Encrypted] key lives under a disjoint identifier and is never reachable from this call.
        await _keyStorage.DeleteFor("test-store", "test-namespace", Subject);
        _releasedAfterPIIErasure = await _compliance.Release("test-store", "test-namespace", _schema, _encrypted);
    }

    [Fact] void should_encrypt_the_pii_property() => Value(_encrypted, "email").ShouldNotEqual(Email);
    [Fact] void should_encrypt_the_encrypted_property() => Value(_encrypted, "apiKey").ShouldNotEqual(ApiKey);
    [Fact] void should_use_different_ciphertext_for_the_two_properties() => Value(_encrypted, "email").ShouldNotEqual(Value(_encrypted, "apiKey"));

    [Fact] async Task should_have_erased_the_pii_key_under_the_bare_subject_identity() =>
        (await _keyStorage.HasFor("test-store", "test-namespace", Subject)).ShouldBeFalse();

    [Fact] async Task should_leave_the_encrypted_key_untouched_under_its_disjoint_identity() =>
        (await _keyStorage.HasFor("test-store", "test-namespace", EncryptedValueKeyIdentifiers.ForSubject(Subject))).ShouldBeTrue();

    [Fact] void should_release_the_pii_property_before_erasure() => Value(_released, "email").ShouldEqual(Email);
    [Fact] void should_release_the_encrypted_property_before_erasure() => Value(_released, "apiKey").ShouldEqual(ApiKey);

    [Fact] void should_erase_the_pii_property_after_pii_key_deletion() => Value(_releasedAfterPIIErasure, "email").ShouldEqual(string.Empty);

    [Fact] void should_leave_the_encrypted_property_readable_after_pii_key_deletion() =>
        Value(_releasedAfterPIIErasure, "apiKey").ShouldEqual(ApiKey);

    static string Value(ExpandoObject instance, string property) =>
        ((IDictionary<string, object?>)instance)[property]!.ToString()!;
}
