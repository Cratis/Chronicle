// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_round_tripping_unsigned_64_bit_references : Specification
{
    IDictionary<string, object?> _read;
    JsonObject _written;

    void Because()
    {
        var schema = JsonSchema.FromJson("""
            {"type":"object","$defs":{"Counter":{"type":"integer","format":"uint64"}},
             "properties":{"value":{"$ref":"#/$defs/Counter"},"values":{"type":"array","items":{"$ref":"#/$defs/Counter"}}}}
            """);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var content = JsonNode.Parse("""{"value":18446744073709551615,"values":[18446744073709551615]}""")!.AsObject();
        var read = converter.ToExpandoObject(content, schema);
        _read = read;
        _written = converter.ToJsonObject(read, schema);
    }

    [Fact] void should_read_the_named_primitive_as_uint64() => _read["value"].ShouldEqual(ulong.MaxValue);
    [Fact] void should_read_the_named_array_element_as_uint64() => ((object[])_read["values"]!)[0].ShouldEqual(ulong.MaxValue);
    [Fact] void should_write_the_named_primitive_without_loss() => _written["value"]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue);
    [Fact] void should_write_the_named_array_element_without_loss() => _written["values"]![0]!.GetValue<ulong>().ShouldEqual(ulong.MaxValue);
}
