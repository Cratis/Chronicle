// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using EncryptionKeyIdentifier = Cratis.Chronicle.Compliance.EncryptionKeyIdentifier;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption;

/// <summary>
/// Describes how an <see cref="EncryptionKeyIdentifier"/> is stored in the identifier columns.
/// </summary>
/// <remarks>
/// SQL Server ignores trailing spaces when it compares strings, under every collation. An identifier that ends with a
/// space therefore gets one terminator character appended, which keeps every stored value free of trailing spaces
/// and makes the comparison exact. Other providers store the identifier unchanged.
/// </remarks>
internal static class EncryptionKeyIdentifierColumn
{
    /// <summary>
    /// The collation of the identifier columns on SQL Server. It compares by code point.
    /// </summary>
    internal const string Collation = "Latin1_General_100_BIN2";

    /// <summary>
    /// The maximum length of the identifier columns.
    /// </summary>
    internal const int MaxLength = 256;

    /// <summary>
    /// The character appended to a value that ends with a space or with the character itself.
    /// </summary>
    internal const string Terminator = "\u0001";

    /// <summary>
    /// Gets the value to store and compare for an identifier.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> the value is used with.</param>
    /// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/>.</param>
    /// <returns>The value for the identifier column.</returns>
    internal static string Value(DbContext context, EncryptionKeyIdentifier identifier) =>
        context.Database.IsSqlServer() && NeedsTerminator(identifier.Value) ? identifier.Value + Terminator : identifier.Value;

    static bool NeedsTerminator(string value) => value.EndsWith(' ') || value.EndsWith(Terminator[0]);
}
