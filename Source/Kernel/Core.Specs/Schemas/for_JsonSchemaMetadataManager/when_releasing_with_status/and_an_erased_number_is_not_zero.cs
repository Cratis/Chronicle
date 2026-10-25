// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_releasing_with_status;

public class and_an_erased_number_is_not_zero : given.a_manager
{
    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{
                "number":{"type":"integer","compliance":[{"metadataType":"PII","details":""}]},
                "readableNumber":{"oneOf":[{"type":"number"},{"type":"null"}],"security":[{"metadataType":"EncryptedGlobal","details":""}]},
                "readableBoolean":{"type":"boolean","security":[{"metadataType":"EncryptedGlobal","details":""}]},
                "readableEmpty":{"type":"string","security":[{"metadataType":"EncryptedGlobal","details":""}]},
                "readableArray":{"type":"array","items":{"type":"integer"},"security":[{"metadataType":"EncryptedGlobal","details":""}]},
                "readableObject":{"type":"object","properties":{"value":{"type":"integer"}},"security":[{"metadataType":"EncryptedGlobal","details":""}]},
                "plain":{"type":"string","compliance":[{"metadataType":"PII","details":""}]}}}
            """);
        _source = JsonNode.Parse("""{"number":57,"readableNumber":0,"readableBoolean":false,"readableEmpty":"","readableArray":[1],"readableObject":{"value":2},"plain":""}""")!.AsObject();
        await ProtectAndErase();
        _protected["plain"] = string.Empty;
    }

    async Task Because() => _result = await _manager.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _protected);

    [Fact] void should_report_erased() => _result.Statuses["number"].ShouldEqual(ProtectedValueReleaseStatus.Erased);
    [Fact] void should_retain_ciphertext_rather_than_zero() => _result.Content["number"]!.GetValue<string>().ShouldEqual(_protected["number"]!.GetValue<string>());
    [Fact] void should_restore_a_decrypted_number() => _result.Content["readableNumber"]!.GetValue<int>().ShouldEqual(0);
    [Fact] void should_restore_a_decrypted_boolean() => _result.Content["readableBoolean"]!.GetValue<bool>().ShouldBeFalse();
    [Fact] void should_preserve_a_legitimate_decrypted_empty_string() => _result.Content["readableEmpty"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_restore_a_decrypted_array() => _result.Content["readableArray"]!.AsArray()[0]!.GetValue<int>().ShouldEqual(1);
    [Fact] void should_restore_a_decrypted_object() => _result.Content["readableObject"]!["value"]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_report_successful_decryption() => _result.Statuses["readableNumber"].ShouldEqual(ProtectedValueReleaseStatus.Decrypted);
    [Fact] void should_distinguish_a_plain_empty_value() => _result.Statuses["plain"].ShouldEqual(ProtectedValueReleaseStatus.Plain);
    [Fact] void should_not_mutate_the_input() => _protected["readableNumber"]!.GetValueKind().ShouldEqual(System.Text.Json.JsonValueKind.String);
}
