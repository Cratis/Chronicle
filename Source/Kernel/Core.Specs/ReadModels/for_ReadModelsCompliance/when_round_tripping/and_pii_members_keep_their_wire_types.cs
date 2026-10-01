// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping;

public class and_pii_members_keep_their_wire_types : Specification
{
    const string Subject = "member-wire-types";
    static readonly Guid _identifier = Guid.Parse("5daf1d70-59f3-44bb-9789-dc00f1966f99");
    static readonly DateTime _registered = new(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    Member _beforeErasure;
    Member _afterErasure;
    JsonObject _stored;
    JsonObject _rewritten;
    InMemoryEncryptionKeyStorage _keys;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "Name": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
                "Address": {
                  "type": "object",
                  "properties": {
                    "Street": { "type": "string", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "PostalCode": { "type": "integer", "format": "int32", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "Verified": { "type": "boolean", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "Identifier": { "type": "string", "format": "guid", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "Registered": { "type": "string", "format": "date-time", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "Balance": { "type": "number", "format": "decimal", "compliance": [{ "metadataType": "PII", "details": "" }] },
                    "Kind": { "type": "integer", "enum": [0, 1], "x-enumNames": ["Unknown", "Home"], "compliance": [{ "metadataType": "PII", "details": "" }] }
                  }
                },
                "Scores": { "type": "array", "items": { "type": "integer", "format": "int32", "compliance": [{ "metadataType": "PII", "details": "" }] } },
                "History": { "type": "array", "items": { "type": "object", "properties": { "Value": { "type": "integer", "compliance": [{ "metadataType": "PII", "details": "" }] } } } },
                "PrivateObject": { "type": "object", "properties": { "Value": { "type": "integer" } }, "compliance": [{ "metadataType": "PII", "details": "" }] },
                "PrivateArray": { "type": "array", "items": { "type": "integer" }, "compliance": [{ "metadataType": "PII", "details": "" }] },
                "Status": { "type": "string" }
              }
            }
            """);
        _keys = new InMemoryEncryptionKeyStorage();
        var encryption = new Encryption();
        var manager = new JsonSchemaMetadataManager(
            new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(_keys, encryption), _keys, encryption)),
            NullLogger<JsonSchemaMetadataManager>.Instance);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var compliance = new ReadModelsCompliance(manager, converter);
        var state = JsonSerializer.SerializeToNode(new Member("Ada Lovelace", new Address("St James's Square", 1815, true, _identifier, _registered, 12345.67890123456789m, AddressKind.Home), [42, 73], [new Entry(91)], new Entry(101), [102, 103], "registered"))!.AsObject();
        var stored = await compliance.Apply("store", "Default", schema, Subject, converter.ToExpandoObject(state, schema));
        _stored = converter.ToJsonObject(stored, schema);
        _stored["__subject"] = Subject;
        _beforeErasure = (await compliance.ReleaseJson("store", "Default", schema, _stored)).Deserialize<Member>()!;

        await _keys.RecordErasureFor("store", "Default", Subject);
        await _keys.DeleteFor("store", "Default", Subject);
        var released = await compliance.ReleaseJson("store", "Default", schema, _stored);
        released["Status"] = "updated";
        _rewritten = await manager.ApplyToReadModel("store", "Default", schema, Subject, released);
        _rewritten["__subject"] = Subject;
        _afterErasure = (await compliance.ReleaseJson("store", "Default", schema, _rewritten)).Deserialize<Member>()!;
    }

    [Fact] void should_release_the_name_before_erasure() => _beforeErasure.Name.ShouldEqual("Ada Lovelace");
    [Fact] void should_release_the_postal_code_before_erasure() => _beforeErasure.Address.PostalCode.ShouldEqual(1815);
    [Fact] void should_release_the_flag_before_erasure() => _beforeErasure.Address.Verified.ShouldBeTrue();
    [Fact] void should_release_the_enum_before_erasure() => _beforeErasure.Address.Kind.ShouldEqual(AddressKind.Home);
    [Fact] void should_release_the_identifier_before_erasure() => _beforeErasure.Address.Identifier.ShouldEqual(_identifier);
    [Fact] void should_release_the_date_before_erasure() => _beforeErasure.Address.Registered.ShouldEqual(_registered);
    [Fact] void should_release_the_decimal_without_precision_loss() => _beforeErasure.Address.Balance.ShouldEqual(12345.67890123456789m);
    [Fact] void should_release_the_scalar_array_before_erasure() => _beforeErasure.Scores.ShouldEqual([42, 73]);
    [Fact] void should_release_the_object_array_before_erasure() => _beforeErasure.History[0].Value.ShouldEqual(91);
    [Fact] void should_release_the_whole_object_before_erasure() => _beforeErasure.PrivateObject.Value.ShouldEqual(101);
    [Fact] void should_release_the_whole_array_before_erasure() => _beforeErasure.PrivateArray.ShouldEqual([102, 103]);
    [Fact] void should_store_the_name_encrypted_before_erasure() => _stored["Name"]!.GetValue<string>().ShouldNotEqual("Ada Lovelace");
    [Fact] void should_store_the_postal_code_encrypted_before_erasure() => _stored["Address"]!["PostalCode"]!.GetValue<string>().ShouldNotEqual("1815");
    [Fact] void should_store_the_flag_encrypted_before_erasure() => IsEncrypted(_stored["Address"]!["Verified"]!).ShouldBeTrue();
    [Fact] void should_store_the_identifier_encrypted_before_erasure() => IsEncrypted(_stored["Address"]!["Identifier"]!).ShouldBeTrue();
    [Fact] void should_store_the_date_encrypted_before_erasure() => IsEncrypted(_stored["Address"]!["Registered"]!).ShouldBeTrue();
    [Fact] void should_store_the_decimal_encrypted_before_erasure() => IsEncrypted(_stored["Address"]!["Balance"]!).ShouldBeTrue();
    [Fact] void should_store_the_enum_encrypted_before_erasure() => IsEncrypted(_stored["Address"]!["Kind"]!).ShouldBeTrue();
    [Fact] void should_store_the_scalar_array_encrypted_before_erasure() => IsEncrypted(_stored["Scores"]![0]!).ShouldBeTrue();
    [Fact] void should_store_the_object_array_encrypted_before_erasure() => IsEncrypted(_stored["History"]![0]!["Value"]!).ShouldBeTrue();
    [Fact] void should_store_the_whole_object_encrypted_before_erasure() => IsEncrypted(_stored["PrivateObject"]!).ShouldBeTrue();
    [Fact] void should_store_the_whole_array_encrypted_before_erasure() => IsEncrypted(_stored["PrivateArray"]!).ShouldBeTrue();
    [Fact] void should_erase_the_name_after_the_update() => _afterErasure.Name.ShouldEqual(string.Empty);
    [Fact] void should_erase_the_street_after_the_update() => _afterErasure.Address.Street.ShouldEqual(string.Empty);
    [Fact] void should_erase_the_postal_code_after_the_update() => _afterErasure.Address.PostalCode.ShouldEqual(0);
    [Fact] void should_erase_the_flag_after_the_update() => _afterErasure.Address.Verified.ShouldBeFalse();
    [Fact] void should_erase_the_enum_after_the_update() => _afterErasure.Address.Kind.ShouldEqual(AddressKind.Unknown);
    [Fact] void should_erase_the_identifier_after_the_update() => _afterErasure.Address.Identifier.ShouldEqual(Guid.Empty);
    [Fact] void should_erase_the_date_after_the_update() => _afterErasure.Address.Registered.ShouldEqual(default);
    [Fact] void should_erase_the_decimal_after_the_update() => _afterErasure.Address.Balance.ShouldEqual(0m);
    [Fact] void should_erase_the_scalar_array_after_the_update() => _afterErasure.Scores.ShouldEqual([0, 0]);
    [Fact] void should_erase_the_object_array_after_the_update() => _afterErasure.History[0].Value.ShouldEqual(0);
    [Fact] void should_erase_the_whole_object_after_the_update() => _afterErasure.PrivateObject.Value.ShouldEqual(0);
    [Fact] void should_erase_the_whole_array_after_the_update() => _afterErasure.PrivateArray.ShouldBeEmpty();
    [Fact] void should_store_the_non_personal_update() => _afterErasure.Status.ShouldEqual("updated");
    [Fact] void should_store_the_name_as_erased() => _rewritten["Name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_postal_code_as_erased() => _rewritten["Address"]!["PostalCode"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_flag_as_erased() => _rewritten["Address"]!["Verified"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_enum_as_erased() => _rewritten["Address"]!["Kind"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_identifier_as_erased() => _rewritten["Address"]!["Identifier"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_date_as_erased() => _rewritten["Address"]!["Registered"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_decimal_as_erased() => _rewritten["Address"]!["Balance"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_scalar_array_as_erased() => _rewritten["Scores"]![0]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_object_array_as_erased() => _rewritten["History"]![0]!["Value"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_whole_object_as_erased() => _rewritten["PrivateObject"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_whole_array_as_erased() => _rewritten["PrivateArray"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] async Task should_not_recreate_the_key() => (await _keys.HasFor("store", "Default", Subject)).ShouldBeFalse();

    static bool IsEncrypted(JsonNode value) => ProtectedValueCodec.TryDecodeCipherText(new Encryption(), value.GetValue<string>(), out _);

    record Member(string Name, Address Address, int[] Scores, Entry[] History, Entry PrivateObject, int[] PrivateArray, string Status);
    record Address(string Street, int PostalCode, bool Verified, Guid Identifier, DateTime Registered, decimal Balance, AddressKind Kind);
    record Entry(int Value);
    enum AddressKind
    {
        Unknown = 0,
        Home = 1
    }
}
