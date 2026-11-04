// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Encodes ordered schema-typed components using decision 0019's v2 contract.
/// This pure component is deliberately not wired into production constraint processing.
/// </summary>
internal static class UniqueConstraintEncoder
{
    const int StackBufferSize = 512;
    const int HashSize = 32;

    /// <summary>
    /// Gets the version of this encoding contract.
    /// </summary>
    internal static HashVersion Version => HashVersion.V2;

    /// <summary>
    /// Produces a keyed value, or no claim when every component is null.
    /// </summary>
    /// <param name="key">The store HMAC key; ownership and provisioning belong to the caller.</param>
    /// <param name="components">The schema-converted properties, in declaration order.</param>
    /// <param name="ignoreCasing">Whether to lowercase string leaves.</param>
    /// <returns>A 64-character lowercase hex value, or no claim.</returns>
    /// <exception cref="UnsupportedUniqueConstraintFormat">A format or nested array is unsupported.</exception>
    /// <exception cref="NonFiniteUniqueConstraintNumber">A number is not finite.</exception>
    internal static Option<UniqueConstraintValue> Encode(ReadOnlySpan<byte> key, IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing = false) =>
        Encode(key, null, components, ignoreCasing);

    /// <summary>
    /// Produces a keyed value with reusable HMAC state, or no claim when every component is null.
    /// </summary>
    /// <param name="hmac">The reusable HMAC state, which must be keyed with the store key and must not be shared between threads.</param>
    /// <param name="components">The schema-converted properties, in declaration order.</param>
    /// <param name="ignoreCasing">Whether to lowercase string leaves.</param>
    /// <returns>A 64-character lowercase hex value, or no claim.</returns>
    /// <exception cref="UnsupportedUniqueConstraintFormat">A format or nested array is unsupported.</exception>
    /// <exception cref="NonFiniteUniqueConstraintNumber">A number is not finite.</exception>
    internal static Option<UniqueConstraintValue> Encode(IncrementalHash hmac, IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing = false) =>
        Encode(default, hmac, components, ignoreCasing);

    /// <summary>
    /// Builds the unhashed v2 preimage: a version prefix, the component count, then each framed component.
    /// </summary>
    /// <param name="components">The schema-converted properties, in declaration order.</param>
    /// <param name="ignoreCasing">Whether to lowercase string leaves.</param>
    /// <returns>The preimage bytes.</returns>
    /// <exception cref="UnsupportedUniqueConstraintFormat">A format or nested array is unsupported.</exception>
    /// <exception cref="NonFiniteUniqueConstraintNumber">A number is not finite.</exception>
    internal static byte[] GetPreimage(IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing = false)
    {
        Span<byte> initial = stackalloc byte[StackBufferSize];
        var writer = new UniqueConstraintPreimageWriter(initial);
        try
        {
            WritePreimage(ref writer, components, ignoreCasing);
            return writer.Written.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    static Option<UniqueConstraintValue> Encode(ReadOnlySpan<byte> key, IncrementalHash? hmac, IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing)
    {
        if (AllNull(components))
        {
            for (var i = 0; i < components.Count; i++)
            {
                UniqueConstraintCanonicalText.ValidateFormat(components[i].Format);
            }
            return Option<UniqueConstraintValue>.None();
        }

        Span<byte> initial = stackalloc byte[StackBufferSize];
        var writer = new UniqueConstraintPreimageWriter(initial);
        try
        {
            WritePreimage(ref writer, components, ignoreCasing);
            Span<byte> hash = stackalloc byte[HashSize];
            UniqueConstraintHmac.Compute(key, hmac, writer.Written, hash);
            return new UniqueConstraintValue(Convert.ToHexStringLower(hash));
        }
        finally
        {
            writer.Dispose();
        }
    }

    static bool AllNull(IReadOnlyList<UniqueConstraintComponent> components)
    {
        for (var i = 0; i < components.Count; i++)
        {
            if (components[i].Value is not null)
            {
                return false;
            }
        }
        return true;
    }

    static void WritePreimage(ref UniqueConstraintPreimageWriter writer, IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing)
    {
        writer.Write("chronicle/unique/v2\0"u8);
        writer.WriteCount(components.Count);
        for (var i = 0; i < components.Count; i++)
        {
            UniqueConstraintFrame.WriteComponent(ref writer, components[i], ignoreCasing);
        }
    }
}
