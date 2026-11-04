// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Writes the tagged, UTF-8, big-endian framing shared by value and scope preimages.
/// </summary>
internal static class UniqueConstraintFrame
{
    static readonly ConcurrentDictionary<Type, bool> _nestedCollections = new();

    /// <summary>
    /// Writes text as a null marker (0x00), or a present marker (0x01), a length and the UTF-8 bytes.
    /// </summary>
    /// <param name="target">The writer to write to.</param>
    /// <param name="text">The text, or null.</param>
    internal static void WriteText(ref UniqueConstraintPreimageWriter target, string? text)
    {
        if (text is null)
        {
            target.WriteByte(0x00);
            return;
        }
        target.WriteUtf8(text);
    }

    /// <summary>
    /// Writes one component as a scalar or, for a collection, a tagged array of scalars.
    /// </summary>
    /// <param name="target">The writer to write to.</param>
    /// <param name="component">The component to write.</param>
    /// <param name="ignoreCasing">Whether to lowercase string leaves.</param>
    /// <exception cref="UnsupportedUniqueConstraintFormat">The format or a nested array is unsupported.</exception>
    internal static void WriteComponent(ref UniqueConstraintPreimageWriter target, UniqueConstraintComponent component, bool ignoreCasing)
    {
        UniqueConstraintCanonicalText.ValidateFormat(component.Format);
        if (component.Value is IEnumerable collection and not string)
        {
            WriteArray(ref target, collection, component, ignoreCasing);
            return;
        }
        WriteScalar(ref target, component.Value, component, ignoreCasing);
    }

    static void WriteArray(ref UniqueConstraintPreimageWriter target, IEnumerable collection, UniqueConstraintComponent component, bool ignoreCasing)
    {
        if (collection is Array { Rank: > 1 } || _nestedCollections.GetOrAdd(collection.GetType(), HasNestedCollections))
        {
            throw new UnsupportedUniqueConstraintFormat("nested array");
        }
        if (collection is object?[] objects)
        {
            target.WriteByte(0x02);
            target.WriteCount(objects.Length);
            for (var i = 0; i < objects.Length; i++)
            {
                WriteScalar(ref target, objects[i], component, ignoreCasing);
            }
            return;
        }
        if (collection is ICollection sized)
        {
            target.WriteByte(0x02);
            target.WriteCount(sized.Count);
            foreach (var element in collection)
            {
                WriteScalar(ref target, element, component, ignoreCasing);
            }
            return;
        }
        var elements = collection.Cast<object?>().ToArray();
        target.WriteByte(0x02);
        target.WriteCount(elements.Length);
        foreach (var element in elements)
        {
            WriteScalar(ref target, element, component, ignoreCasing);
        }
    }

    static bool HasNestedCollections(Type type) => type.GetInterfaces().Any(HasCollectionElements);

    static bool HasCollectionElements(Type type) => type.IsGenericType &&
        type.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
        type.GetGenericArguments()[0] != typeof(string) &&
        typeof(IEnumerable).IsAssignableFrom(type.GetGenericArguments()[0]);

    static void WriteScalar(ref UniqueConstraintPreimageWriter target, object? value, UniqueConstraintComponent component, bool ignoreCasing)
    {
        if (value is IEnumerable and not string)
        {
            throw new UnsupportedUniqueConstraintFormat("nested array");
        }
        var text = value is null ? null : UniqueConstraintCanonicalText.For(value, component.Format, component.IsInSchema, ignoreCasing);
        WriteText(ref target, text);
    }
}
