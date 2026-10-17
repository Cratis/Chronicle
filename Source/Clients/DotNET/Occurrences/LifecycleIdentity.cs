// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Mirrors the kernel's length-prefixed lifecycle identity for observing graceful shutdown completion.
/// </summary>
internal static class LifecycleIdentity
{
    /// <summary>
    /// Derives the stable shutdown identity in a delivery scope.
    /// </summary>
    /// <param name="kind">The lowercase lifecycle kind.</param>
    /// <param name="activationId">The logical activation.</param>
    /// <param name="namespace">The namespace, or null for application scope.</param>
    /// <returns>The stable identity.</returns>
    internal static string Calculate(string kind, ActivationId activationId, string? @namespace)
    {
        var parts = @namespace is null ? new[] { kind, activationId.Value } : [kind, activationId.Value, @namespace];
        var encoded = string.Concat(parts.Select(part => $"{part.Length.ToString(CultureInfo.InvariantCulture)}:{part}|"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(encoded)));
    }
}
