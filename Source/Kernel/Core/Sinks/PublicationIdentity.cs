// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Computes deterministic, opaque identities for event publications.
/// </summary>
/// <remarks>
/// A part is length-prefixed so different splits of the same characters can never produce the same input.
/// </remarks>
public static class PublicationIdentity
{
    /// <summary>
    /// Computes the identity of one processing step of one observer in one replay occurrence.
    /// </summary>
    /// <param name="parts">The ordered identity parts: store, namespace, destination, observer, occurrence, target key and input position.</param>
    /// <returns>The opaque identity.</returns>
    public static string For(params string[] parts) => Hash(parts);

    /// <summary>
    /// Computes a digest of everything that makes up an intent, so a retry that differs can be told from a retry that does not.
    /// </summary>
    /// <param name="parts">The ordered parts of the intent.</param>
    /// <returns>The opaque digest.</returns>
    public static string Fingerprint(params string[] parts) => Hash(parts);

    static string Hash(string[] parts)
    {
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            builder.Append(part.Length).Append(':').Append(part).Append('|');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}
