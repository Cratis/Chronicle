// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_a_legacy_client_registers_a_protected_property : Specification
{
    JsonSchema _stored;
    JsonSchema _incoming;
    JsonSchema _refined;
    bool _compatible;
    bool _protected;

    void Establish()
    {
        _stored = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"default":null,"type":"number","format":"decimal","compliance":[{"metadataType":"PII","details":""}],"security":[{"metadataType":"Encrypted","details":""}]}}}""");
        _incoming = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"default":null}}}""");
    }

    void Because()
    {
        _compatible = _stored.IsCompatibleWith(_incoming);
        _protected = _stored.HasCompatibleProtectionMetadata(_incoming);
        _refined = _stored.MorePrecise(_incoming);
    }

    [Fact] void should_accept_the_legacy_schema() => _compatible.ShouldBeTrue();
    [Fact] void should_inherit_protection_metadata() => _protected.ShouldBeTrue();
    [Fact] void should_retain_the_stored_schema() => _refined.ToJson().ShouldEqual(_stored.ToJson());
}
