// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Buffers.Binary;
using System.Dynamic;
using System.Text;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Writes the closed, schema-normalized content tree without using an Orleans reference session.
/// Partial output is discarded by the caller when a node is unsupported or exceeds the depth limit.
/// </summary>
/// <param name="buffer">The disposable staging buffer; never the Orleans session buffer.</param>
internal sealed class CompactContentWriter(IBufferWriter<byte> buffer)
{
    const int MaximumDepth = 64;
    static readonly UTF8Encoding _utf8 = new(false, true);

    /// <summary>
    /// Attempts to stage the complete content tree, preserving types, key casing and enumeration order.
    /// </summary>
    /// <param name="content">The normalized content.</param>
    /// <returns>False when the caller must discard the staging buffer and use the legacy codec.</returns>
    internal bool TryWrite(ExpandoObject content)
    {
        try
        {
            return TryWriteNode(content, 0);
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }

    bool TryWriteNode(object? value, int depth)
    {
        if (depth > MaximumDepth)
        {
            return false;
        }
        switch (value)
        {
            case null:
                WriteTag(CompactContentTag.Null);
                return true;
            case ExpandoObject nested:
                return TryWriteObject(nested, depth);
            case object[] array:
                return TryWriteArray(array, depth);
            default:
                return TryWriteScalar(value);
        }
    }

    bool TryWriteScalar(object value)
    {
        switch (value)
        {
            case string text:
                WriteTag(CompactContentTag.String);
                WriteString(text);
                return true;
            case bool flag:
                WriteTag(CompactContentTag.Boolean);
                WriteByte(flag ? (byte)1 : (byte)0);
                return true;
            case byte[] bytes:
                WriteTag(CompactContentTag.Bytes);
                WriteLength(bytes.Length);
                WriteBytes(bytes);
                return true;
            default:
                return TryWriteExtendedScalar(value);
        }
    }

    bool TryWriteExtendedScalar(object value)
    {
        if (value is decimal number)
        {
            WriteDecimal(number);
            return true;
        }
        if (value is Guid id)
        {
            WriteTag(CompactContentTag.Guid);
            id.TryWriteBytes(buffer.GetSpan(16));
            buffer.Advance(16);
            return true;
        }
        return TryWriteTemporal(value) || TryWriteNumber(value);
    }

    bool TryWriteObject(ExpandoObject value, int depth)
    {
        var properties = (IDictionary<string, object?>)value;
        WriteTag(CompactContentTag.Object);
        WriteLength(properties.Count);
        foreach (var property in properties)
        {
            WriteString(property.Key);
            if (!TryWriteNode(property.Value, depth + 1))
            {
                return false;
            }
        }
        return true;
    }

    bool TryWriteArray(object?[] value, int depth)
    {
        WriteTag(CompactContentTag.Array);
        WriteLength(value.Length);
        foreach (var item in value)
        {
            if (!TryWriteNode(item, depth + 1))
            {
                return false;
            }
        }
        return true;
    }

    bool TryWriteNumber(object value) => TryWriteSigned(value) || TryWriteUnsigned(value) || TryWriteFloatingPoint(value);

    bool TryWriteSigned(object value) => WriteNumber(value switch
    {
        sbyte item => (CompactContentTag.SByte, item),
        short item => (CompactContentTag.Int16, item),
        int item => (CompactContentTag.Int32, item),
        long item => (CompactContentTag.Int64, item),
        _ => null
    });

    bool TryWriteUnsigned(object value) => WriteNumber(value switch
    {
        byte item => (CompactContentTag.Byte, item),
        ushort item => (CompactContentTag.UInt16, item),
        uint item => (CompactContentTag.UInt32, item),
        ulong item => (CompactContentTag.UInt64, unchecked((long)item)),
        _ => null
    });

    bool TryWriteFloatingPoint(object value) => WriteNumber(value switch
    {
        float item => (CompactContentTag.Single, BitConverter.SingleToInt32Bits(item)),
        double item => (CompactContentTag.Double, BitConverter.DoubleToInt64Bits(item)),
        _ => null
    });

    bool WriteNumber((CompactContentTag Tag, long Bits)? number)
    {
        if (number is not { } encoded)
        {
            return false;
        }
        WriteTag(encoded.Tag);
        WriteInt64(encoded.Bits);
        return true;
    }

    bool TryWriteTemporal(object value)
    {
        switch (value)
        {
            case DateTime item:
                WriteTag(CompactContentTag.DateTime);
                WriteInt64(item.Ticks);
                WriteByte((byte)item.Kind);
                return true;
            case DateTimeOffset item:
                WriteTag(CompactContentTag.DateTimeOffset);
                WriteInt64(item.Ticks);
                WriteInt64(item.Offset.Ticks);
                return true;
            case DateOnly item:
                WriteTag(CompactContentTag.DateOnly);
                WriteInt64(item.DayNumber);
                return true;
            case TimeOnly item:
                WriteTag(CompactContentTag.TimeOnly);
                WriteInt64(item.Ticks);
                return true;
            case TimeSpan item:
                WriteTag(CompactContentTag.TimeSpan);
                WriteInt64(item.Ticks);
                return true;
            default:
                return false;
        }
    }

    void WriteDecimal(decimal value)
    {
        WriteTag(CompactContentTag.Decimal);
        Span<int> bits = stackalloc int[4];
        decimal.GetBits(value, bits);
        foreach (var part in bits)
        {
            WriteInt64(part);
        }
    }

    void WriteString(string value)
    {
        var length = _utf8.GetByteCount(value);
        WriteLength(length);
        _utf8.GetBytes(value, buffer.GetSpan(length));
        buffer.Advance(length);
    }

    void WriteLength(int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer.GetSpan(4), value);
        buffer.Advance(4);
    }

    void WriteInt64(long value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(buffer.GetSpan(8), value);
        buffer.Advance(8);
    }

    void WriteTag(CompactContentTag tag) => WriteByte((byte)tag);

    void WriteByte(byte value)
    {
        buffer.GetSpan(1)[0] = value;
        buffer.Advance(1);
    }

    void WriteBytes(ReadOnlySpan<byte> value)
    {
        value.CopyTo(buffer.GetSpan(value.Length));
        buffer.Advance(value.Length);
    }
}
