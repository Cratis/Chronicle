// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Identifies one effect across every attempt of an occurrence. Pass Value to external idempotent endpoints.
/// </summary>
/// <param name="Occurrence">The occurrence identity.</param>
/// <param name="Name">The stable effect name.</param>
/// <param name="WrittenBy">The attempt writing the receipt; not part of equality.</param>
public sealed record EffectKey(OccurrenceId Occurrence, EffectName Name, LeaseToken WrittenBy)
{
    /// <summary>
    /// Gets the opaque, namespace-scoped external idempotency key.
    /// </summary>
    public string Value => $"effect:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Encode(Occurrence.Value) + Encode(Name.Value))))}";

    /// <summary>
    /// Compares occurrence and name, independently of the writing lease.
    /// </summary>
    /// <param name="other">The other effect key.</param>
    /// <returns>Whether both identify the same effect.</returns>
    public bool Equals(EffectKey? other) => other is not null && Occurrence == other.Occurrence && Name == other.Name;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Occurrence, Name);

    static string Encode(string part) => $"{part.Length.ToString(CultureInfo.InvariantCulture)}:{part}|";
}
