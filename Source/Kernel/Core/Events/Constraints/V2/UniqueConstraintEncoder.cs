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
    internal static Option<UniqueConstraintValue> Encode(ReadOnlySpan<byte> key, IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing = false)
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
    /// Builds the unhashed v2 preimage: a version prefix, the component count, then each framed component.
    /// </summary>
    /// <param name="components">The schema-converted properties, in declaration order.</param>
    /// <param name="ignoreCasing">Whether to lowercase string leaves.</param>
    /// <returns>The preimage bytes.</returns>
    /// <exception cref="UnsupportedUniqueConstraintFormat">A format or nested array is unsupported.</exception>
    /// <exception cref="NonFiniteUniqueConstraintNumber">A number is not finite.</exception>
    internal static byte[] GetPreimage(IReadOnlyList<UniqueConstraintComponent> components, bool ignoreCasing = false)
    {
        using var target = new MemoryStream();
        target.Write("chronicle/unique/v2\0"u8);
        UniqueConstraintFrame.WriteCount(target, components.Count);
        foreach (var component in components)
        {
            UniqueConstraintFrame.WriteComponent(target, component, ignoreCasing);
        }
        return target.ToArray();
    }
}
