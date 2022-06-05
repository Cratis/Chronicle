// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Aksio.Cratis.Guids;

/// <summary>
/// Provides a set of extension methods for working with <see cref="Guid"/>.
/// </summary>
public static class GuidExtensions
{
    /// <summary>
    /// Xor one Guid with another and return the result.
    /// </summary>
    /// <param name="a">First Guid.</param>
    /// <param name="b">Second Guid.</param>
    /// <returns>Xor'ed Guid.</returns>
    /// <remarks>
    /// Found here: https://stackoverflow.com/a/63719049.
    /// </remarks>
    public static Guid Xor(this Guid a, Guid b)
    {
        if (Sse2.IsSupported)
        {
            var result = Sse2.Xor(Unsafe.As<Guid, Vector128<long>>(ref a), Unsafe.As<Guid, Vector128<long>>(ref b));
            return Unsafe.As<Vector128<long>, Guid>(ref result);
        }

        var spanA = MemoryMarshal.CreateSpan(ref Unsafe.As<Guid, long>(ref a), 2);
        var spanB = MemoryMarshal.CreateSpan(ref Unsafe.As<Guid, long>(ref b), 2);

        spanB[0] ^= spanA[0];
        spanB[1] ^= spanA[1];

        return b;
    }
}
