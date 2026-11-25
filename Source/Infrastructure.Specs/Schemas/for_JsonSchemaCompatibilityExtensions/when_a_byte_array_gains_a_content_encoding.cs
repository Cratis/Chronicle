// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions;

public class when_a_byte_array_gains_a_content_encoding : Specification
{
    JsonSchema _net10;
    JsonSchema _net11;
    bool _added;
    bool _removed;
    bool _equalForStorage;

    void Establish()
    {
        _net10 = JsonSchema.FromJson("""
            {"type":"object","properties":{"PhotoData":{"type":"string","format":"byte-array"}}}
            """);
        _net11 = JsonSchema.FromJson("""
            {"type":"object","properties":{"PhotoData":{"type":"string","contentEncoding":"base64","format":"byte-array"}}}
            """);
    }

    void Because()
    {
        _added = _net10.IsCompatibleWith(_net11);
        _removed = _net11.IsCompatibleWith(_net10);
        _equalForStorage = JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(_net10.ToJson(), _net11.ToJson());
    }

    [Fact] void should_accept_the_net11_schema_for_a_net10_generation() => _added.ShouldBeTrue();
    [Fact] void should_accept_the_net10_schema_for_a_net11_generation() => _removed.ShouldBeTrue();
    [Fact] void should_ignore_the_content_encoding_for_storage_equality() => _equalForStorage.ShouldBeTrue();
}
