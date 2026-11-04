// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;

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
    /// The name the System event store's databases were named by before <see cref="Configuration.Storage.SystemDatabaseName"/>
    /// existed: the event store name itself.
    /// </summary>
    public const string LegacySystemDatabaseName = "System";

    /// <summary>
    /// The maximum length, in bytes, MongoDB allows for a database name.
    /// </summary>
    const int MaximumLengthInBytes = 63;

    /// <summary>
    /// The reserved physical name space that real event stores, namespaces and prefixes cannot alias.
    /// </summary>
    const string ReservedPhysicalMarker = "!chronicle-encrypted!";

    /// <summary>
    /// The characters MongoDB does not allow in a database name.
    /// </summary>
    static readonly SearchValues<char> _invalidCharacters = SearchValues.Create(['/', '\\', '.', ' ', '"', '$', '*', '<', '>', ':', '|', '?', '\0']);

    /// <summary>
    /// Prepend the configured prefix and validate the complete database name.
    /// </summary>
    /// <param name="databaseName">The unprefixed database name.</param>
    /// <param name="prefix">The prefix, or empty to preserve the database name.</param>
    /// <returns>The validated database name.</returns>
    public static string WithPrefix(string databaseName, string prefix = "") =>
        Validated($"{prefix}{databaseName}", new EventStoreName(databaseName), null);

    /// <summary>
    /// Get the name of the database holding an event store's cross-namespace state.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get the database name for.</param>
    /// <param name="prefix">The prefix, or empty to preserve the database name.</param>
    /// <param name="systemDatabaseName">The name the System event store's databases are named by.</param>
    /// <returns>The database name.</returns>
    public static string ForEventStore(EventStoreName eventStore, string prefix = "", string systemDatabaseName = Configuration.Storage.DefaultSystemDatabaseName)
    {
        NameFor(eventStore, systemDatabaseName);
        return Validated(ComposeForEventStore(eventStore, prefix, systemDatabaseName), eventStore, null);
    }

    /// <summary>
    /// Get the name of the database holding the event sequences of a namespace within an event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get the database name for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to get the database name for.</param>
    /// <param name="prefix">The prefix, or empty to preserve the database name.</param>
    /// <param name="systemDatabaseName">The name the System event store's databases are named by.</param>
    /// <returns>The database name.</returns>
    public static string ForEventStoreNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace, string prefix = "", string systemDatabaseName = Configuration.Storage.DefaultSystemDatabaseName)
    {
        NameFor(eventStore, systemDatabaseName);
        return Validated(ComposeForEventStoreNamespace(eventStore, @namespace, prefix, systemDatabaseName), eventStore, @namespace);
    }

    /// <summary>
    /// Get the name of the database read models of a namespace within an event store are materialized into.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get the database name for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to get the database name for.</param>
    /// <param name="prefix">The prefix, or empty to preserve the database name.</param>
    /// <param name="systemDatabaseName">The name the System event store's databases are named by.</param>
    /// <returns>The database name.</returns>
    /// <remarks>
    /// Unlike the event sequence databases, the default namespace is not suffixed — its read models live in the
    /// bare event store name. A reader that suffixes unconditionally therefore resolves a database that simply
    /// does not exist for the default namespace, and reads come back empty rather than failing.
    /// </remarks>
    /// <exception cref="InvalidDatabaseName">The composed name is not usable, including when it is the cluster database.</exception>
    public static string ForReadModels(EventStoreName eventStore, EventStoreNamespaceName @namespace, string prefix = "", string systemDatabaseName = Configuration.Storage.DefaultSystemDatabaseName)
    {
        NameFor(eventStore, systemDatabaseName);
        var databaseName = ComposeForReadModels(eventStore, @namespace, prefix, systemDatabaseName);
        if (databaseName.Equals(ComposeClusterDatabase(prefix), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDatabaseName(databaseName, "it is the database Chronicle keeps its cluster state in", eventStore, @namespace.Value);
        }

        return Validated(databaseName, eventStore, @namespace);
    }

    /// <summary>
    /// Compose the name of the database holding an event store's cross-namespace state, without validating it.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to compose the database name for.</param>
    /// <param name="prefix">The prefix, or empty to preserve the database name.</param>
    /// <param name="systemDatabaseName">The name the System event store's databases are named by.</param>
    /// <returns>The composed database name.</returns>
    public static string ComposeForEventStore(EventStoreName eventStore, string prefix = "", string systemDatabaseName = Configuration.Storage.DefaultSystemDatabaseName) =>
        $"{prefix}{PhysicalNameFor(eventStore, systemDatabaseName)}+es";

    /// <summary>
    /// Compose the name of the database holding the event sequences of a namespace, without validating it.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to compose the database name for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to compose the database name for.</param>
    /// <param name="prefix">The prefix, or empty to preserve the database name.</param>
    /// <param name="systemDatabaseName">The name the System event store's databases are named by.</param>
    /// <returns>The composed database name.</returns>
    public static string ComposeForEventStoreNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace, string prefix = "", string systemDatabaseName = Configuration.Storage.DefaultSystemDatabaseName) =>
        $"{prefix}{PhysicalNameFor(eventStore, systemDatabaseName)}+es+{@namespace}";

    /// <summary>
    /// Compose the name of the database read models of a namespace are materialized into, without validating it.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to compose the database name for.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to compose the database name for.</param>
    /// <param name="prefix">The prefix, or empty to preserve the database name.</param>
    /// <param name="systemDatabaseName">The name the System event store's databases are named by.</param>
    /// <returns>The composed database name.</returns>
    public static string ComposeForReadModels(EventStoreName eventStore, EventStoreNamespaceName @namespace, string prefix = "", string systemDatabaseName = Configuration.Storage.DefaultSystemDatabaseName)
    {
        var name = PhysicalNameFor(eventStore, systemDatabaseName);
        return @namespace == EventStoreNamespaceName.Default ? $"{prefix}{name}" : $"{prefix}{name}+{@namespace}";
    }

    /// <summary>
    /// Compose the name of the database holding the cluster state, without validating it.
    /// </summary>
    /// <param name="prefix">The prefix, or empty to preserve the database name.</param>
    /// <returns>The composed database name.</returns>
    public static string ComposeClusterDatabase(string prefix = "") => $"{prefix}{WellKnownDatabaseNames.Chronicle}";

    /// <summary>
    /// Get the event store a name found in place of the event store name in a database name stands for.
    /// </summary>
    /// <param name="name">The name as it appears in the database name.</param>
    /// <param name="systemDatabaseName">The name the System event store's databases are named by.</param>
    /// <returns>The <see cref="EventStoreName"/>.</returns>
    public static EventStoreName EventStoreFor(string name, string systemDatabaseName = Configuration.Storage.DefaultSystemDatabaseName) =>
        string.Equals(name, systemDatabaseName, StringComparison.OrdinalIgnoreCase) ? EventStoreName.System : new EventStoreName(name);

    static string NameFor(EventStoreName eventStore, string systemDatabaseName)
    {
        if (string.Equals(eventStore.Value, EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase))
        {
            return systemDatabaseName;
        }

        if (string.Equals(eventStore.Value, systemDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDatabaseName(eventStore.Value, "the name is reserved for the System event store's databases", eventStore, null);
        }

        return eventStore.Value;
    }

    static string PhysicalNameFor(EventStoreName eventStore, string systemDatabaseName) =>
        string.Equals(eventStore.Value, EventStoreName.System.Value, StringComparison.OrdinalIgnoreCase) ? systemDatabaseName : eventStore.Value;

    static string Validated(string databaseName, EventStoreName eventStore, EventStoreNamespaceName? @namespace)
    {
        if (eventStore == EncryptedValueKeyIdentifiers.GlobalEventStore)
        {
            databaseName = databaseName.Replace(EncryptedValueKeyIdentifiers.GlobalEventStore.Value, ReservedPhysicalMarker, StringComparison.Ordinal);
        }
        else if (databaseName.Contains(ReservedPhysicalMarker, StringComparison.Ordinal))
        {
            throw new InvalidDatabaseName(databaseName, "it uses the physical name space reserved for confidentiality keys", eventStore, @namespace?.Value);
        }

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
