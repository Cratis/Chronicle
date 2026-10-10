// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_checking_schema_maps_with_reserved_property_names : Specification
{
    JsonSchema _stored;
    JsonSchema _incoming;
    bool _compatible;
    JsonSchema _precise;

    void Establish()
    {
        _stored = JsonSchema.FromJson("""{"type":"object","properties":{"compliance":{"type":"string","default":null},"security":{"type":"number","format":"decimal","default":null},"default":{"type":"string","default":null},"title":{"type":"string"}},"$defs":{"security":{"type":"string"}}}""");
        _incoming = JsonSchema.FromJson("""{"type":"object","properties":{"compliance":{"default":null},"security":{"default":null},"default":{"default":null},"title":{"type":"string"}},"$defs":{"security":{"type":"string"}}}""");
    }

    void Because()
    {
        _compatible = _stored.IsCompatibleWith(_incoming);
        _precise = _stored.MorePrecise(_incoming);
    }

    [Fact] void should_treat_names_as_property_names() => _compatible.ShouldBeTrue();
    [Fact] void should_keep_the_precise_properties() => _precise.ToJson().ShouldEqual(_stored.ToJson());
    [Fact] void should_not_report_property_names_as_metadata() => _stored.AddedProtectionMetadataPaths(_incoming).ShouldBeEmpty();
}
