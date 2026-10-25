// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

public class when_round_tripping_an_erased_referenced_guid : Specification
{
    static readonly Guid _plainGuid = Guid.Parse("531ff0ef-d2c2-468b-b576-66e4d350a28c");
    IDictionary<string, object?> _read;
    JsonObject _written;

    void Because()
    {
        var schema = JsonSchema.FromJson("""
            {
                "type": "object",
                "$defs": {
                    "ProtectedGuid": {
                        "type": "string",
                        "format": "guid",
                        "compliance": [{ "metadataType": "PII", "details": "" }]
                    }
                },
                "properties": {
                    "protectedId": { "$ref": "#/$defs/ProtectedGuid" },
                    "plainId": { "type": "string", "format": "guid" }
                }
            }
            """);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var read = converter.ToExpandoObject(
            new JsonObject { ["protectedId"] = string.Empty, ["plainId"] = _plainGuid.ToString() },
            schema);
        _read = read;
        _written = converter.ToJsonObject(read, schema);
    }

    [Fact] void should_keep_the_erased_marker_opaque_on_read() => _read["protectedId"].ShouldEqual(string.Empty);
    [Fact] void should_keep_the_erased_marker_opaque_on_write() => _written["protectedId"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_parse_the_unprotected_guid_on_read() => _read["plainId"].ShouldEqual(_plainGuid);
    [Fact] void should_write_the_unprotected_guid() => _written["plainId"]!.GetValue<Guid>().ShouldEqual(_plainGuid);
}
