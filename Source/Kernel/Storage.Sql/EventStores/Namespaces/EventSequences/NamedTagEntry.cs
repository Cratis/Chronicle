// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// A named tag belonging to one event in one sequence in an event-store namespace.
/// </summary>
#pragma warning disable CA1819 // EF Core requires byte-array properties for portable binary SQL columns.
public class NamedTagEntry
{
    /// <summary>Gets or sets the event sequence table name.</summary>
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the event sequence number.</summary>
    public ulong SequenceNumber { get; set; }

    /// <summary>Gets or sets the position of this tag within the event.</summary>
    public int Position { get; set; }

    /// <summary>Gets or sets the UTF-8 name bytes.</summary>
    public byte[] Name { get; set; } = [];

    /// <summary>Gets or sets the UTF-8 value bytes.</summary>
    public byte[] Value { get; set; } = [];

    /// <summary>Gets or sets the SHA-256 name digest.</summary>
    public byte[] NameHash { get; set; } = [];

    /// <summary>Gets or sets the SHA-256 value digest.</summary>
    public byte[] ValueHash { get; set; } = [];

    /// <summary>Gets or sets the schema ownership marker.</summary>
    public int CratisNamedTagsVersion { get; set; } = 1;

    /// <summary>Creates an entry from a validated named tag.</summary>
    /// <param name="sequenceId">The event sequence identity.</param>
    /// <param name="number">The event number.</param>
    /// <param name="position">The tag's position.</param>
    /// <param name="tag">The tag to persist.</param>
    /// <returns>The entry.</returns>
    public static NamedTagEntry From(string sequenceId, ulong number, int position, NamedTag tag)
    {
        var name = Encoding.UTF8.GetBytes(tag.Name.Value);
        var value = Encoding.UTF8.GetBytes(tag.Value);
        return new()
        {
            EventSequenceId = sequenceId,
            SequenceNumber = number,
            Position = position,
            Name = name,
            Value = value,
            NameHash = SHA256.HashData(name),
            ValueHash = SHA256.HashData(value)
        };
    }

    /// <summary>Returns the original tag without applying a database collation.</summary>
    /// <returns>The named tag.</returns>
    public NamedTag ToNamedTag() => new(new TagName(Encoding.UTF8.GetString(Name)), Encoding.UTF8.GetString(Value));
}
#pragma warning restore CA1819
