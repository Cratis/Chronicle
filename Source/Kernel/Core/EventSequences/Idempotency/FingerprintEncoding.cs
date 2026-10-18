// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// The invariant v1 field encoding. Lengths count UTF-16 characters, exactly like PublicationIdentity.
/// </summary>
internal static class FingerprintEncoding
{
    /// <summary>
    /// Names a protection kind using its persistent spelling.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The name, or null for an unknown kind.</returns>
    internal static string? Name(ProtectionKind kind) => kind switch
    {
        ProtectionKind.Pii => "pii",
        ProtectionKind.EncryptedSubject => "encrypted-subject",
        ProtectionKind.EncryptedNamespace => "encrypted-namespace",
        ProtectionKind.EncryptedGlobal => "encrypted-global",
        _ => null
    };

    /// <summary>
    /// Encodes unambiguous length-prefixed fields.
    /// </summary>
    /// <param name="parts">The ordered fields.</param>
    /// <returns>The encoded text.</returns>
    internal static string Fields(IEnumerable<string> parts)
    {
        var builder = new StringBuilder();
        foreach (var part in parts)
        {
            builder.Append(part.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(part).Append('|');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Checks the fixed lowercase SHA-256 representation.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <returns>Whether it is exactly one lowercase digest.</returns>
    internal static bool IsDigest(string value) => value.Length == 64 && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
