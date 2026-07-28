// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents the rules by which Chronicle names the MongoDB databases it stores its data in.
/// </summary>
/// <remarks>
/// This is the one place the naming is decided. Anything resolving a Chronicle database — inside the kernel or
/// outside it — must go through here rather than composing the name again, since a second copy of the rule is
/// free to drift and a database name that does not match is not an error but an empty result.
/// </remarks>
public static class DatabaseNames
{
    /// <summary>
    /// The maximum length, in bytes, MongoDB allows for a database name.
    /// </summary>
    const int MaximumLengthInBytes = 63;

    /// <summary>
    /// The characters MongoDB does not allow in a database name.
    /// </summary>
    static readonly SearchValues<char> _invalidCharacters = SearchValues.Create(['/', '\\', '.', ' ', '"', '$', '*', '<', '>', ':', '|', '?', '\0']);

    /// <summary>
    /// Get the name of the database holding an event store's cross-namespace state.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get the database name for.</param>
    /// <returns>The database name.</returns>
    public static string ForEventStore(EventStoreName eventStore) =>
        Validated($"{eventStore}+es", eventStore, null);

    /// <summary>
    /// Get the name of the database holding the event sequences of a namespace within an event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get the database name for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to get the database name for.</param>
    /// <returns>The database name.</returns>
    public static string ForEventStoreNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        Validated($"{eventStore}+es+{@namespace}", eventStore, @namespace);

    /// <summary>
    /// Get the name of the database read models of a namespace within an event store are materialized into.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get the database name for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to get the database name for.</param>
    /// <returns>The database name.</returns>
    /// <remarks>
    /// Unlike the event sequence databases, the default namespace is not suffixed — its read models live in the
    /// bare event store name. A reader that suffixes unconditionally therefore resolves a database that simply
    /// does not exist for the default namespace, and reads come back empty rather than failing.
    /// </remarks>
    public static string ForReadModels(EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        Validated(
            @namespace == EventStoreNamespaceName.Default ? $"{eventStore}" : $"{eventStore}+{@namespace}",
            eventStore,
            @namespace);

    static string Validated(string databaseName, EventStoreName eventStore, EventStoreNamespaceName? @namespace)
    {
        var reason = GetInvalidReason(databaseName);
        if (reason is not null)
        {
            throw new InvalidDatabaseName(databaseName, reason, eventStore, @namespace?.Value);
        }

        return databaseName;
    }

    static string? GetInvalidReason(string databaseName)
    {
        if (string.IsNullOrEmpty(databaseName))
        {
            return "a database name cannot be empty";
        }

        var invalidCharacter = databaseName.AsSpan().IndexOfAny(_invalidCharacters);
        if (invalidCharacter >= 0)
        {
            return $"it contains '{Describe(databaseName[invalidCharacter])}' at position {invalidCharacter}, which MongoDB does not allow in a database name (neither are / \\ . \" $ * < > : | ? or the null character)";
        }

        var length = Encoding.UTF8.GetByteCount(databaseName);
        return length > MaximumLengthInBytes
            ? $"it is {length} bytes long and MongoDB allows at most {MaximumLengthInBytes}"
            : null;
    }

    static string Describe(char character) => character switch
    {
        ' ' => "a space",
        '\0' => "a null character",
        _ => character.ToString()
    };
}
