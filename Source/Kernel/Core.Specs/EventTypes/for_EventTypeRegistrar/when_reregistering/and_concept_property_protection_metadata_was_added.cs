// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_reregistering;

public class and_concept_property_protection_metadata_was_added : given.a_registration
{
    const string StoredSchema = """
        {"type":"object","properties":{
            "note":{"type":"string"},
            "subject":{"type":"string"},
            "namespace":{"type":"string"},
            "global":{"type":"string"}
        }}
        """;
    const string IncomingSchema = """
        {"type":"object","properties":{
            "note":{"type":"string","compliance":[{"metadataType":"PII","details":"Property details"}]},
            "subject":{"type":"string","security":[{"metadataType":"EncryptedSubject","details":"Subject details"}]},
            "namespace":{"type":"string","security":[{"metadataType":"EncryptedNamespace","details":"Namespace details"}]},
            "global":{"type":"string","security":[{"metadataType":"EncryptedGlobal","details":"Global details"}]}
        }}
        """;

    void Establish() => StoredEventTypes(StoredEventType("decimal", (1, StoredSchema)));

    Task Because() => Register(IncomingSchema);

    [Fact] void should_register_the_same_generation_without_a_schema_change_error() => _registered.Definition.Generations.Single().Generation.Value.ShouldEqual(1u);
    [Fact] void should_persist_the_added_pii_metadata() => _registered.Definition.Generations.Single().Schema.ActualProperties["note"].GetComplianceMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata("PII", "Property details"));
    [Fact] void should_persist_the_added_subject_encryption_metadata() => _registered.Definition.Generations.Single().Schema.ActualProperties["subject"].GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata("EncryptedSubject", "Subject details"));
    [Fact] void should_persist_the_added_namespace_encryption_metadata() => _registered.Definition.Generations.Single().Schema.ActualProperties["namespace"].GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata("EncryptedNamespace", "Namespace details"));
    [Fact] void should_persist_the_added_global_encryption_metadata() => _registered.Definition.Generations.Single().Schema.ActualProperties["global"].GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata("EncryptedGlobal", "Global details"));
}
