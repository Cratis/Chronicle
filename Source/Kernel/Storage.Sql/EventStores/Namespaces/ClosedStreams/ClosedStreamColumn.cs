// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams;

/// <summary>
/// Describes how the stream type, stream id, event source id and event source type of a closed stream are stored.
/// </summary>
/// <remarks>
/// On SQL Server these columns use a binary collation, so letter case, character width and accents count. SQL Server
/// still ignores trailing spaces when it compares strings, under every collation. A value that ends with a space or
/// with the terminator therefore gets one terminator character appended, which keeps every stored value free of
/// trailing spaces and makes the comparison exact. Other providers store the value unchanged.
/// </remarks>
internal static class ClosedStreamColumn
{
    /// <summary>
    /// The collation of the identifier columns on SQL Server. It compares by code point.
    /// </summary>
    internal const string Collation = EventSequences.ExactIdentifiers.BinaryCollation;

    /// <summary>
    /// The maximum length of the identifier columns on SQL Server, one more than the 255 characters a value can have so the terminator fits.
    /// </summary>
    internal const int MaxLength = 256;

    /// <summary>
    /// The character appended to a value that ends with a space or with the character itself.
    /// </summary>
    internal const char Terminator = '\u0001';

    /// <summary>
    /// Gets the value to store and compare for an identifier.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> the value is used with.</param>
    /// <param name="value">The identifier as the caller spells it.</param>
    /// <returns>The value for the column.</returns>
    internal static string Encode(DbContext context, string value) =>
        context.Database.IsSqlServer() && (value.EndsWith(' ') || value.EndsWith(Terminator)) ? value + Terminator : value;

    /// <summary>
    /// Gets the identifier from a stored column value.
    /// </summary>
    /// <param name="context">The <see cref="DbContext"/> the value was read with.</param>
    /// <param name="stored">The stored column value.</param>
    /// <returns>The identifier as the caller spelled it.</returns>
    internal static string Decode(DbContext context, string stored) =>
        context.Database.IsSqlServer() && stored.EndsWith(Terminator) ? stored[..^1] : stored;
}
