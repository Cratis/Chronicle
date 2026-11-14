// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintCanonicalText.when_canonicalizing;

public class and_date_time_values_have_different_offsets : Specification
{
    string _withOffset;
    string _inUtc;

    void Because()
    {
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"moment":{"type":"string","format":"date-time"}}}""");
        _withOffset = Canonicalize(converter, schema, "2025-01-02T03:04:05.1234567+05:30");
        _inUtc = Canonicalize(converter, schema, "2025-01-01T21:34:05.1234567Z");
    }

    [Fact] void should_canonicalize_the_same_instant_to_the_same_text() => _withOffset.ShouldEqual(_inUtc);
    [Fact] void should_canonicalize_to_the_utc_instant() => _withOffset.ShouldEqual("2025-01-01T21:34:05.1234567Z");

    static string Canonicalize(ExpandoObjectConverter converter, JsonSchema schema, string value)
    {
        var expando = converter.ToExpandoObject(Encoding.UTF8.GetBytes($$"""{"moment":"{{value}}"}"""), schema);
        return UniqueConstraintCanonicalText.For(((IDictionary<string, object?>)expando)["moment"]!, "date-time", true, false);
    }
}
