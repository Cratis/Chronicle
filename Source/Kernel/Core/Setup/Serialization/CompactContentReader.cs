// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Dynamic;
using System.Text;

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// Reads version-1 content with payload-bounded counts, lengths and nesting.
/// </summary>
/// <param name="content">The complete content payload.</param>
internal ref struct CompactContentReader(ReadOnlySpan<byte> content)
{
    const int MaximumDepth = 64;
    static readonly UTF8Encoding _utf8 = new(false, true);
    ReadOnlySpan<byte> _remaining = content;

    /// <summary>
    /// Reads exactly one root object and requires the complete payload to be consumed.
    /// </summary>
    /// <returns>The typed content tree.</returns>
    /// <exception cref="InvalidDataException">The content is incomplete, invalid or has trailing bytes.</exception>
    internal ExpandoObject Read()
    {
        var result = ReadNode(0) as ExpandoObject ?? throw new InvalidDataException("Compact content must be an object.");
        if (!_remaining.IsEmpty) throw new InvalidDataException("Trailing compact content.");
        return result;
    }

    static object Box<T>(T value)
        where T : struct => value;

    object? ReadNode(int depth)
    {
        if (depth > MaximumDepth) throw new InvalidDataException("Compact content nesting limit exceeded.");
        var tag = (CompactContentTag)Take(1)[0];
        return tag switch
        {
            CompactContentTag.Null => null,
            CompactContentTag.Object => ReadObject(depth),
            CompactContentTag.Array => ReadArray(depth),
            _ => ReadLeaf(tag)
        };
    }

    object ReadLeaf(CompactContentTag tag) => tag switch
    {
        CompactContentTag.String => _utf8.GetString(Take(ReadLength())),
        CompactContentTag.Boolean => ReadBoolean(),
        CompactContentTag.Bytes => Take(ReadLength()).ToArray(),
        CompactContentTag.Decimal => ReadDecimal(),
        CompactContentTag.Guid => new Guid(Take(16)),
        _ => ReadScalar(tag)
    };

    ExpandoObject ReadObject(int depth)
    {
        var count = ReadLength();
        IDictionary<string, object?> result = new ExpandoObject();
        for (var index = 0; index < count; index++)
        {
            var key = _utf8.GetString(Take(ReadLength()));
            if (!result.TryAdd(key, ReadNode(depth + 1))) throw new InvalidDataException("Duplicate compact content key.");
        }
        return (ExpandoObject)result;
    }

    object?[] ReadArray(int depth)
    {
        var count = ReadLength();
        var result = new object?[count];
        for (var index = 0; index < count; index++) result[index] = ReadNode(depth + 1);
        return result;
    }

    object ReadScalar(CompactContentTag tag) => tag switch
    {
        CompactContentTag.DateTime => new DateTime(ReadInt64(), (DateTimeKind)Take(1)[0]),
        CompactContentTag.DateTimeOffset => new DateTimeOffset(ReadInt64(), new TimeSpan(ReadInt64())),
        CompactContentTag.DateOnly => DateOnly.FromDayNumber(checked((int)ReadInt64())),
        CompactContentTag.TimeOnly => new TimeOnly(ReadInt64()),
        CompactContentTag.TimeSpan => new TimeSpan(ReadInt64()),
        _ => ReadNumber(tag)
    };

    object ReadNumber(CompactContentTag tag)
    {
        if (tag is CompactContentTag.Single or CompactContentTag.Double)
        {
            return ReadFloatingPoint(tag);
        }
        return ReadInteger(tag);
    }

    object ReadInteger(CompactContentTag tag) => tag switch
    {
        CompactContentTag.SByte or CompactContentTag.Int16 or CompactContentTag.Int32 or CompactContentTag.Int64 => ReadSigned(tag),
        _ => ReadUnsigned(tag)
    };

    object ReadSigned(CompactContentTag tag) => tag switch
    {
        CompactContentTag.SByte => Box(checked((sbyte)ReadInt64())),
        CompactContentTag.Int16 => Box(checked((short)ReadInt64())),
        CompactContentTag.Int32 => Box(checked((int)ReadInt64())),
        CompactContentTag.Int64 => Box(ReadInt64()),
        _ => throw new InvalidDataException("Unknown compact signed integer tag.")
    };

    object ReadUnsigned(CompactContentTag tag) => tag switch
    {
        CompactContentTag.Byte => Box(checked((byte)ReadInt64())),
        CompactContentTag.UInt16 => Box(checked((ushort)ReadInt64())),
        CompactContentTag.UInt32 => Box(checked((uint)ReadInt64())),
        CompactContentTag.UInt64 => Box(unchecked((ulong)ReadInt64())),
        _ => throw new InvalidDataException("Unknown compact unsigned integer tag.")
    };

    object ReadFloatingPoint(CompactContentTag tag) => tag switch
    {
        CompactContentTag.Single => Box(BitConverter.Int32BitsToSingle(checked((int)ReadInt64()))),
        CompactContentTag.Double => Box(BitConverter.Int64BitsToDouble(ReadInt64())),
        _ => throw new InvalidDataException("Unknown compact floating point tag.")
    };

    decimal ReadDecimal()
    {
        Span<int> bits = stackalloc int[4];
        for (var index = 0; index < bits.Length; index++) bits[index] = checked((int)ReadInt64());
        return new decimal(bits);
    }

    bool ReadBoolean()
    {
        var value = Take(1)[0];
        if (value > 1) throw new InvalidDataException("Invalid compact boolean.");
        return value != 0;
    }

    int ReadLength()
    {
        var length = BinaryPrimitives.ReadInt32LittleEndian(Take(4));
        if ((uint)length > (uint)_remaining.Length) throw new InvalidDataException("Invalid compact content length.");
        return length;
    }

    long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(Take(8));

    ReadOnlySpan<byte> Take(int length)
    {
        if ((uint)length > (uint)_remaining.Length) throw new InvalidDataException("Truncated compact content.");
        var result = _remaining[..length];
        _remaining = _remaining[length..];
        return result;
    }
}
