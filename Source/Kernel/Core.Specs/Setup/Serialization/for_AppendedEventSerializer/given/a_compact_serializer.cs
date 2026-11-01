// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.Session;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer.given;

public class a_compact_serializer : a_normalized_event
{
    protected ChronicleOptions configuration;

    void Establish()
    {
        configuration = service_provider.GetRequiredService<IOptions<ChronicleOptions>>().Value;
        configuration.Clustering.EventWireFormat = EventWireFormat.Compact;
    }

    protected WireType WireTypeOf(byte[] bytes)
    {
        using var session = service_provider.GetRequiredService<SerializerSessionPool>().GetSession();
        var reader = Reader.Create(bytes, session);
        return reader.ReadFieldHeader().WireType;
    }

    protected static ExpandoObject ContentWith(params (string Key, object? Value)[] properties)
    {
        IDictionary<string, object?> result = new ExpandoObject();
        foreach (var (key, value) in properties)
        {
            result.Add(key, value);
        }
        return (ExpandoObject)result;
    }

    protected byte[] CompactMessage(uint version, byte[]? content, bool extraField = false)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var session = service_provider.GetRequiredService<SerializerSessionPool>().GetSession();
        var writer = Writer.Create(buffer, session);
        ReferenceCodec.MarkValueField(session);
        writer.WriteFieldHeaderExpected(0, WireType.TagDelimited);
        UInt32Codec.WriteField(ref writer, 0, version);
        Blob(ref writer, 1, JsonSerializer.SerializeToUtf8Bytes(appended_event, options));
        if (content is not null) Blob(ref writer, 1, content);
        if (extraField) Blob(ref writer, 1, [255]);
        writer.WriteEndObject();
        writer.Commit();
        return buffer.WrittenSpan.ToArray();
    }

    static void Blob(ref Writer<ArrayBufferWriter<byte>> writer, uint delta, byte[] bytes)
    {
        ReferenceCodec.MarkValueField(writer.Session);
        writer.WriteFieldHeaderExpected(delta, WireType.LengthPrefixed);
        writer.WriteVarUInt32((uint)bytes.Length);
        writer.Write(bytes);
    }

    protected void AssertEquivalent(AppendedEvent actual, AppendedEvent expected)
    {
        JsonSerializer.Serialize(actual, options).ShouldEqual(JsonSerializer.Serialize(expected, options));
        ContentTypes(actual.Content).ShouldEqual(ContentTypes(expected.Content));
    }
}
