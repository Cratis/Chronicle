// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_classifying_metadata;

public class and_contextual_candidates_do_not_select_additional_handlers : when_releasing_with_status.given.a_manager
{
    JsonSchemaProperty _property;
    (SchemaMetadataCategory Category, ComplianceSchemaMetadata Metadata)[] _metadata;

    void Establish()
    {
        _schema = JsonSchema.FromJson("""
            {
              "$id":"https://schemas.example/document",
              "properties":{"value":{"$dynamicRef":"base#personal","compliance":[{"metadataType":"PII","details":""}]}},
              "$defs":{
                "base":{"$id":"base","$dynamicAnchor":"personal","type":"string"},
                "candidate":{"$id":"candidate","$dynamicAnchor":"personal","type":"string","security":[{"metadataType":"EncryptedGlobal","details":""}]}
              }
            }
            """);
        _property = _schema.Properties["value"];
    }

    async Task Because()
    {
        _metadata = _manager.MetadataFor(_property, forComparison: true);
        _source = new() { ["value"] = "sensitive" };
        _protected = await _manager.Apply(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _source);
        _result = await _manager.ReleaseWithStatus(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _schema, Identifier, _protected);
    }

    [Fact] void should_discover_the_possible_security_boundary() => _property.HasSecurityMetadata().ShouldBeTrue();
    [Fact] void should_keep_only_the_selected_handler() => _metadata.Length.ShouldEqual(1);
    [Fact] void should_keep_the_selected_pii_handler() => _metadata[0].Category.ShouldEqual(SchemaMetadataCategory.Compliance);
    [Fact] void should_still_encrypt_the_selected_boundary() => _protected["value"].GetValue<string>().ShouldNotEqual("sensitive");
    [Fact] void should_keep_the_one_handler_status_guard_satisfied() => _result.Statuses["value"].ShouldEqual(ProtectedValueReleaseStatus.Decrypted);
    [Fact] void should_release_the_original_value() => _result.Content["value"].GetValue<string>().ShouldEqual("sensitive");
}
