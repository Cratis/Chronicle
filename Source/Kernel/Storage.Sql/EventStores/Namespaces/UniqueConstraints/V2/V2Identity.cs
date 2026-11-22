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
    internal const string Authorities = "UniqueConstraintAuthoritiesV2";
    internal const string Bindings = "UniqueConstraintBindingsV2";
    internal const string BinaryCollation = "Latin1_General_100_BIN2";
    internal const int OwnerLimit = 450;

    internal static string Authority(string sequence, string constraint) => Hash(sequence, constraint);

    internal static string Binding(string sequence, string constraint, UniqueConstraintMode mode) => Hash(sequence, constraint, ((int)mode).ToString(System.Globalization.CultureInfo.InvariantCulture));

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
