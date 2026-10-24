// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace Cratis.Chronicle.Tools.GrainCompatibility.Payloads;

/// <summary>
/// Orleans' generated field IDs: xxHash32 of the UTF-16 minimally qualified type and canonical member name.
/// </summary>
internal static class PayloadFieldId
{
    const uint Prime1 = 2654435761;
    const uint Prime2 = 2246822519;
    const uint Prime3 = 3266489917;
    const uint Prime4 = 668265263;
    const uint Prime5 = 374761393;

    internal static uint For(Type type, string name) => BinaryPrimitives.ReverseEndianness(Hash(Encoding.Unicode.GetBytes($"{TypeName(type)}%{name}")));

    static string TypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return TypeName(underlying) + "?";
        }

        if (type.IsArray)
        {
            return TypeName(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        }

        if (type.IsGenericType)
        {
            var name = type.Name.Split('`')[0];
            return name + "<" + string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ">";
        }

        var display = PayloadTypeName.Of(type);
        return display.StartsWith("System.", StringComparison.Ordinal) ? type.Name : display.Split('.')[^1];
    }

    static uint Round(uint accumulator, uint value) => BitOperations.RotateLeft(accumulator + (value * Prime2), 13) * Prime1;

    static uint Hash(ReadOnlySpan<byte> bytes)
    {
        var offset = 0;
        uint hash;
        if (bytes.Length >= 16)
        {
            var a = unchecked(Prime1 + Prime2);
            var b = Prime2;
            uint c = 0;
            var d = unchecked(0U - Prime1);
            while (offset <= bytes.Length - 16)
            {
                a = Round(a, BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]));
                b = Round(b, BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 4)..]));
                c = Round(c, BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 8)..]));
                d = Round(d, BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 12)..]));
                offset += 16;
            }

            hash = BitOperations.RotateLeft(a, 1) + BitOperations.RotateLeft(b, 7) + BitOperations.RotateLeft(c, 12) + BitOperations.RotateLeft(d, 18);
        }
        else
        {
            hash = Prime5;
        }

        hash += (uint)bytes.Length;
        while (offset <= bytes.Length - 4)
        {
            hash = BitOperations.RotateLeft(hash + (BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]) * Prime3), 17) * Prime4;
            offset += 4;
        }

        while (offset < bytes.Length)
        {
            hash = BitOperations.RotateLeft(hash + (bytes[offset++] * Prime5), 11) * Prime1;
        }

        hash = (hash ^ (hash >> 15)) * Prime2;
        hash = (hash ^ (hash >> 13)) * Prime3;
        return hash ^ (hash >> 16);
    }
}
