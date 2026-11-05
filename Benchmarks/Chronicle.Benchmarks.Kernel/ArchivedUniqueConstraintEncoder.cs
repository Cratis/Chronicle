// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// The first, unoptimised v2 encoder, kept as the "archived v2" column of the benchmark.
/// It builds a <see cref="MemoryStream"/> preimage, allocates a byte array per component and hashes through hex strings.
/// </summary>
static class ArchivedUniqueConstraintEncoder
{
    /// <summary>
    /// Encodes the components with the archived v2 encoding.
    /// </summary>
    /// <param name="key">The store HMAC key.</param>
    /// <param name="components">The schema-converted properties, in declaration order.</param>
    /// <param name="ignoreCasing">Whether to lowercase string leaves.</param>
    /// <returns>The keyed value, or no claim when every component is null.</returns>
    public static Option<UniqueConstraintValue> Encode(ReadOnlySpan<byte> key, IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing)
    {
        var preimage = GetPreimage(components, ignoreCasing);
        if (components.All(_ => _.Value is null))
        {
            return Option<UniqueConstraintValue>.None();
        }
        var hash = HMACSHA256.HashData(key, preimage);
        return new UniqueConstraintValue(Convert.ToHexString(hash).ToLowerInvariant());
    }

    /// <summary>
    /// Encodes the scope with the archived v2 encoding.
    /// </summary>
    /// <param name="key">The store HMAC key.</param>
    /// <param name="scope">The typed scope.</param>
    /// <returns>The scope token.</returns>
    public static UniqueConstraintScopeToken EncodeScope(ReadOnlySpan<byte> key, ResolvedConstraintScope? scope)
    {
        using var target = new MemoryStream();
        target.Write("chronicle/scope/v2\0"u8);
        WriteText(target, scope?.EventSourceType?.Value);
        WriteText(target, scope?.EventStreamType?.Value);
        WriteText(target, scope?.EventStreamId?.Value);
        var hash = HMACSHA256.HashData(key, target.ToArray());
        return new(Convert.ToHexString(hash).ToLowerInvariant()[..32]);
    }

    static byte[] GetPreimage(IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing)
    {
        using var target = new MemoryStream();
        target.Write("chronicle/unique/v2\0"u8);
        WriteCount(target, components.Count);
        foreach (var component in components)
        {
            WriteComponent(target, component, ignoreCasing);
        }
        return target.ToArray();
    }

    static void WriteCount(Stream target, int count)
    {
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes, checked((uint)count));
        target.Write(bytes);
    }

    static void WriteText(Stream target, string? text)
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

    static void WriteComponent(Stream target, UniqueConstraintComponent component, bool ignoreCasing)
    {
        UniqueConstraintCanonicalText.ValidateFormat(component.Format);
        if (component.Value is IEnumerable collection and not string)
        {
            var elements = collection.Cast<object?>().ToArray();
            target.WriteByte(0x02);
            WriteCount(target, elements.Length);
            foreach (var element in elements)
            {
                WriteScalar(target, element, component, ignoreCasing);
            }
            return;
        }
        WriteScalar(target, component.Value, component, ignoreCasing);
    }

    static void WriteScalar(Stream target, object? value, UniqueConstraintComponent component, bool ignoreCasing)
    {
        var text = value is null ? null : UniqueConstraintCanonicalText.For(value, component.Format, component.IsInSchema, ignoreCasing);
        WriteText(target, text);
    }
}
