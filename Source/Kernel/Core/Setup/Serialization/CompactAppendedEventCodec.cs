// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Buffers.Adaptors;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Encodes version 1 as a tag-delimited AppendedEvent: version, common-envelope UTF-8 JSON, typed content.
/// Only the outer event participates in reference tracking; both length-prefixed blobs are value fields.
/// </summary>
/// <param name="options">The existing serializer options for the common envelope.</param>
internal sealed class CompactAppendedEventCodec(JsonSerializerOptions options)
{
    /// <summary>
    /// The supported compact envelope version.
    /// </summary>
    internal const uint Version = 1;

    /// <summary>
    /// Writes a compact event, or returns false without touching the session or output for unsupported content.
    /// </summary>
    /// <typeparam name="TBufferWriter">The output buffer type.</typeparam>
    /// <param name="writer">The Orleans writer.</param>
    /// <param name="fieldIdDelta">The outer field delta.</param>
    /// <param name="expectedType">The expected field type.</param>
    /// <param name="value">The event.</param>
    /// <returns>Whether compact encoding was used.</returns>
    internal bool TryWrite<TBufferWriter>(ref Writer<TBufferWriter> writer, uint fieldIdDelta, [AllowNull] Type expectedType, AppendedEvent? value)
        where TBufferWriter : IBufferWriter<byte>
    {
        var buffer = new BufferWriterBox<PooledBuffer>(new PooledBuffer());
        try
        {
            if (value is not null && !new CompactContentWriter(buffer).TryWrite(value.Content))
            {
                return false;
            }
            if (ReferenceCodec.TryWriteReferenceField(ref writer, fieldIdDelta, expectedType, typeof(AppendedEvent), value))
            {
                return true;
            }
            var envelope = JsonSerializer.SerializeToUtf8Bytes(CompactEventEnvelope.From(value), options);
            writer.WriteFieldHeader(fieldIdDelta, expectedType, typeof(AppendedEvent), WireType.TagDelimited);
            UInt32Codec.WriteField(ref writer, 0, Version);
            WriteBlob(ref writer, 1, envelope);
            WriteContent(ref writer, ref buffer.Value);
            writer.WriteEndObject();
            return true;
        }
        finally
        {
            buffer.Value.Dispose();
        }
    }

    /// <summary>
    /// Reads a compact event, rejecting unknown versions and incomplete envelopes.
    /// </summary>
    /// <typeparam name="TInput">The input buffer type.</typeparam>
    /// <param name="reader">The Orleans reader.</param>
    /// <param name="field">The outer field.</param>
    /// <returns>The decoded event.</returns>
    /// <exception cref="InvalidDataException">The version, fields or content are invalid.</exception>
    internal AppendedEvent? ReadValue<TInput>(ref Reader<TInput> reader, Field field)
    {
        if (field.WireType != WireType.TagDelimited)
        {
            throw new InvalidDataException("Invalid compact event wire type.");
        }
        var reference = ReferenceCodec.CreateRecordPlaceholder(reader.Session);
        var fields = new CompactEventFields(options);
        uint fieldId = 0;
        while (true)
        {
            var inner = reader.ReadFieldHeader();
            if (inner.IsEndObject) break;
            fieldId = checked(fieldId + inner.FieldIdDelta);
            fields.Read(ref reader, fieldId, inner);
        }
        var result = fields.Build();
        ReferenceCodec.RecordObject(reader.Session, result, reference);
        return result;
    }

    static void WriteBlob<TBufferWriter>(ref Writer<TBufferWriter> writer, uint delta, ReadOnlySpan<byte> bytes)
        where TBufferWriter : IBufferWriter<byte>
    {
        ReferenceCodec.MarkValueField(writer.Session);
        writer.WriteFieldHeaderExpected(delta, WireType.LengthPrefixed);
        writer.WriteVarUInt32((uint)bytes.Length);
        writer.Write(bytes);
    }

    static void WriteContent<TBufferWriter>(ref Writer<TBufferWriter> writer, ref PooledBuffer content)
        where TBufferWriter : IBufferWriter<byte>
    {
        ReferenceCodec.MarkValueField(writer.Session);
        writer.WriteFieldHeaderExpected(1, WireType.LengthPrefixed);
        writer.WriteVarUInt32(checked((uint)content.Length));
        content.CopyTo(ref writer);
    }
}
