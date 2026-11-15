// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter;

/// <summary>
/// Converting a large state costs memory in proportion to it, and what that proportion is has to stay known: a
/// reducer state that is tens of megabytes is converted several times for every batch of events, and the
/// factor is what decides whether it fits in the heap. This holds the factor to a bound.
/// </summary>
public class when_converting_a_large_state : Specification
{
    /// <summary>
    /// The bounds are the measured factors with headroom: tighten them when the conversion gets cheaper, and when one
    /// fails ask what started allocating, rather than raising it.
    /// </summary>
    const double WriteBound = 19;
    const double ReadBound = 19;
    const int Entries = 20_000;

    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    ExpandoObject _state;
    string _json;
    byte[] _utf8;
    long _writeAllocated;
    long _readAllocated;

    void Establish()
    {
        _converter = new(new TypeFormats());
        _schema = JsonSchema.FromType<StreamedContainer>();

        var entries = new JsonArray();
        for (var i = 0; i < Entries; i++)
        {
            entries.Add(new JsonObject
            {
                ["id"] = Guid.NewGuid(),
                ["name"] = $"A reasonably long name for entry number {i} in the state",
                ["count"] = i,
                ["created"] = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                ["note"] = "some note",
                ["tags"] = new JsonArray("alpha", "beta", "gamma"),
                ["labels"] = new JsonObject { ["x"] = "1", ["y"] = "2" }
            });
        }

        var source = new JsonObject
        {
            ["name"] = "container",
            ["primary"] = entries[0]!.DeepClone(),
            ["entries"] = entries,
            ["aliases"] = new JsonArray("one"),
            ["matrix"] = new JsonArray()
        };

        _state = _converter.ToExpandoObject(source, _schema);
        _json = _converter.ToJsonObject(_state, _schema).ToJsonString();
        _utf8 = LargeJson.ToUtf8(_json);
    }

    void Because()
    {
        // Warm up, so that one-off costs are not counted.
        _ = LargeJson.Serialize(writer => _converter.WriteJson(writer, _state, _schema), new JsonSerializerOptions(), _utf8.Length);
        _ = _converter.ToExpandoObject(_utf8, _schema);

        var before = GC.GetAllocatedBytesForCurrentThread();
        _ = LargeJson.Serialize(writer => _converter.WriteJson(writer, _state, _schema), new JsonSerializerOptions(), _utf8.Length);
        _writeAllocated = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        _ = _converter.ToExpandoObject(_utf8, _schema);
        _readAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact] void should_write_within_a_bounded_multiple_of_the_json() => (_writeAllocated / (double)_utf8.Length).ShouldBeLessThan(WriteBound);
    [Fact] void should_read_within_a_bounded_multiple_of_the_json() => (_readAllocated / (double)_utf8.Length).ShouldBeLessThan(ReadBound);
}
