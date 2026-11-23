// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// Names feature objects from length-framed logical tuples, never from truncated logical identifiers.
/// </summary>
static class V2Identity
{
    /// <summary>
    /// The authority metadata table name.
    /// </summary>
    internal const string Authorities = "UniqueConstraintAuthoritiesV2";

    /// <summary>
    /// The logical binding metadata table name.
    /// </summary>
    internal const string Bindings = "UniqueConstraintBindingsV2";

    /// <summary>
    /// The SQL Server binary collation used for indexed identifiers.
    /// </summary>
    internal const string BinaryCollation = "Latin1_General_100_BIN2";

    /// <summary>
    /// The maximum owner length in UTF-16 code units.
    /// </summary>
    internal const int OwnerLimit = 450;

    /// <summary>
    /// Gets the digest of an authority's full logical identity.
    /// </summary>
    /// <param name="sequence">The sequence identity.</param>
    /// <param name="constraint">The constraint identity.</param>
    /// <returns>The authority lookup key.</returns>
    internal static string Authority(string sequence, string constraint) => Hash(sequence, constraint);

    /// <summary>
    /// Gets the digest of a binding's full logical identity.
    /// </summary>
    /// <param name="sequence">The sequence identity.</param>
    /// <param name="constraint">The constraint identity.</param>
    /// <param name="mode">The retention mode.</param>
    /// <returns>The binding lookup key.</returns>
    internal static string Binding(string sequence, string constraint, UniqueConstraintMode mode) => Hash(sequence, constraint, ((int)mode).ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>
    /// Gets the bounded physical table name for a binding.
    /// </summary>
    /// <param name="binding">The binding lookup key.</param>
    /// <param name="mode">The retention mode.</param>
    /// <returns>The physical claim table name.</returns>
    internal static string Table(string binding, UniqueConstraintMode mode) => $"uc_{binding[..48]}_{(mode == UniqueConstraintMode.PerValue ? "uc2v" : "uc2")}";

    static string Hash(params string[] parts)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(int)];
        foreach (var part in parts)
        {
            var bytes = Encoding.UTF8.GetBytes(part);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
