// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Collections;
using System.Text;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Writes the tagged, UTF-8, big-endian framing shared by value and scope preimages.
/// </summary>
internal static class UniqueConstraintFrame
{
    internal static void WriteCount(Stream target, int count)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, checked((uint)count));
        target.Write(bytes);
    }

    internal static void WriteText(Stream target, string? text)
    {
        if (text is null)
        {
            target.WriteByte(0x00);
            return;
        }
        var bytes = Encoding.UTF8.GetBytes(text);
        target.WriteByte(0x01);
        WriteCount(target, bytes.Length);
        target.Write(bytes);
    }

    internal static void WriteComponent(Stream target, UniqueConstraintComponent component, bool ignoreCasing)
    {
        UniqueConstraintCanonicalText.ValidateFormat(component.Format);
        if (component.Value is IEnumerable collection and not string)
        {
            WriteArray(target, collection, component, ignoreCasing);
            return;
        }
        WriteScalar(target, component.Value, component, ignoreCasing);
    }

    static void WriteArray(Stream target, IEnumerable collection, UniqueConstraintComponent component, bool ignoreCasing)
    {
        if (collection is Array { Rank: > 1 } || collection.GetType().GetInterfaces().Any(HasCollectionElements))
        {
            throw new UnsupportedUniqueConstraintFormat("nested array");
        }
        var elements = collection.Cast<object?>().ToArray();
        target.WriteByte(0x02);
        WriteCount(target, elements.Length);
        foreach (var element in elements)
        {
            WriteScalar(target, element, component, ignoreCasing);
        }
    }

    static bool HasCollectionElements(Type type) => type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
        type.GetGenericArguments()[0] != typeof(string) &&
        typeof(IEnumerable).IsAssignableFrom(type.GetGenericArguments()[0]);

    static void WriteScalar(Stream target, object? value, UniqueConstraintComponent component, bool ignoreCasing)
    {
        if (value is IEnumerable and not string)
        {
            throw new UnsupportedUniqueConstraintFormat("nested array");
        }
        var text = value is null ? null : UniqueConstraintCanonicalText.For(value, component.Format, component.IsInSchema, ignoreCasing);
        WriteText(target, text);
    }
}
