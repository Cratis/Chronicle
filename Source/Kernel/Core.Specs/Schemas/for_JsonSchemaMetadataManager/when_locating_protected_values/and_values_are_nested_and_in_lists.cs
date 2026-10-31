// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_locating_protected_values;

public class and_values_are_nested_and_in_lists : Specification
{
    IEncryptionKeyStorage _storage;
    IManagedEncryptionKeyProvisioner _provisioner;
    JsonSchemaMetadataManager _manager;
    JsonSchema _schema;
    JsonObject _plaintext;
    IReadOnlyList<ProtectedValueLocation> _result;

    async Task Establish()
    {
        _storage = Substitute.For<IEncryptionKeyStorage>();
        _provisioner = Substitute.For<IManagedEncryptionKeyProvisioner>();
        var handler = new PIICompliancePropertyValueHandler(_provisioner, _storage, Substitute.For<IEncryption>());
        _manager = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"nested":{"type":"object","properties":{"secret":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}},"list":{"type":"array","items":{"type":"integer","compliance":[{"metadataType":"PII","details":""}]}}}}""");
        _plaintext = JsonNode.Parse("""{"nested":{"secret":"private"},"list":[42]}""")!.AsObject();
    }

    async Task Because() => _result = await _manager.LocateProtectedValues(_schema, "subject", _plaintext);

    [Fact] void should_not_read_any_key() => _storage.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_provision_any_key() => _provisioner.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_report_canonical_paths() => string.Join(',', _result.Select(_ => _.Path)).ShouldEqual("list[0],nested.secret");
    [Fact] void should_report_the_scoped_identifiers() => _result.All(_ => _.Kind == ProtectionKind.Pii && _.Identifier.Value == "subject").ShouldBeTrue();
    [Fact] void should_report_the_plaintext_values() => string.Join('|', _result.Select(_ => _.Plaintext.ToJsonString())).ShouldEqual("42|\"private\"");
    [Fact] void should_not_mutate_the_input() => _plaintext["nested"]!["secret"]!.GetValue<string>().ShouldEqual("private");
}
