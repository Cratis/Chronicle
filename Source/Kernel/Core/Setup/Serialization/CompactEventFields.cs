// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Orleans.Serialization.Buffers;
using Orleans.Serialization.Codecs;
using Orleans.Serialization.WireProtocol;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Accumulates and validates the required fields of one compact event independently of reference tracking.
/// </summary>
/// <param name="options">The existing JSON options for the common envelope.</param>
internal sealed class CompactEventFields(JsonSerializerOptions options)
{
    bool _hasVersion;
    CompactEventEnvelope? _envelope;
    ExpandoObject? _content;

    /// <summary>
    /// Consumes a known field or skips an unknown optional field using Orleans' reference bookkeeping.
    /// </summary>
    /// <typeparam name="TInput">The input buffer type.</typeparam>
    /// <param name="reader">The Orleans reader.</param>
    /// <param name="fieldId">The absolute field identifier.</param>
    /// <param name="field">The field header.</param>
    internal void Read<TInput>(ref Reader<TInput> reader, uint fieldId, Field field)
    {
        switch (fieldId)
        {
            case 0:
                ReadVersion(ref reader, field);
                break;
            case 1:
                ReadEnvelope(ref reader, field);
                break;
            case 2:
                ReadContent(ref reader, field);
                break;
            default:
                reader.ConsumeUnknownField(field);
                break;
        }
    }

    /// <summary>
    /// Requires all mandatory fields and reconstructs the complete event.
    /// </summary>
    /// <returns>The decoded event.</returns>
    /// <exception cref="InvalidDataException">A mandatory field is missing.</exception>
    internal AppendedEvent Build()
    {
        if (!_hasVersion) throw new InvalidDataException("Missing required compact event field.");
        var envelope = _envelope ?? throw new InvalidDataException("Missing required compact event field.");
        var content = _content ?? throw new InvalidDataException("Missing required compact event field.");
        return envelope.WithContent(content);
    }

    static byte[] ReadBlob<TInput>(ref Reader<TInput> reader, Field field)
    {
        if (field.WireType != WireType.LengthPrefixed)
        {
            throw new InvalidDataException("Invalid compact event field wire type.");
        }
        ReferenceCodec.MarkValueField(reader.Session);
        return reader.ReadBytes(reader.ReadVarUInt32());
    }

    void ReadVersion<TInput>(ref Reader<TInput> reader, Field field)
    {
        if (_hasVersion || UInt32Codec.ReadValue(ref reader, field) != CompactAppendedEventCodec.Version)
        {
            throw new InvalidDataException("Unsupported compact event version.");
        }
        _hasVersion = true;
    }

    void ReadEnvelope<TInput>(ref Reader<TInput> reader, Field field)
    {
        if (_envelope is not null) throw new InvalidDataException("Duplicate compact event envelope.");
        _envelope = JsonSerializer.Deserialize<CompactEventEnvelope>(ReadBlob(ref reader, field), options)
            ?? throw new InvalidDataException("Missing compact event envelope.");
    }

    void ReadContent<TInput>(ref Reader<TInput> reader, Field field)
    {
        if (_content is not null) throw new InvalidDataException("Duplicate compact event content.");
        _content = new CompactContentReader(ReadBlob(ref reader, field)).Read();
    }
}
