// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Finds the existing owners whose storage names would be the same as the storage names of a new owner.
/// </summary>
/// <remarks>
/// All backends compose names as <c language="csharp">{event store}+es</c>, <c language="csharp">{event store}+es+{namespace}</c> and <c language="csharp">{event store}+{namespace}</c>,
/// where the read models of the default namespace use the bare event store name. A new name that follows the naming rule contains no
/// separator, so it can only overlap an existing owner whose name contains one. The comparison ignores case, which is the
/// strictest of the backends.
/// </remarks>
public static class ResourceNameOverlaps
{
    const string DefaultNamespace = "Default";

    /// <summary>
    /// Check whether the storage of a new event store would overlap the storage of an existing event store.
    /// </summary>
    /// <param name="eventStore">The new event store name.</param>
    /// <param name="existingEventStores">The existing event store names.</param>
    /// <returns>True if it would overlap.</returns>
    public static bool ForEventStore(string eventStore, IEnumerable<string> existingEventStores)
    {
        var storeDatabase = eventStore + "+es";
        return existingEventStores.Any(_ => string.Equals(_, storeDatabase, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Check whether the storage of a new namespace would overlap the storage of an existing namespace.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The new namespace name.</param>
    /// <param name="existingNamespaces">The existing namespaces of the event store.</param>
    /// <param name="existingEventStores">All existing event store names.</param>
    /// <param name="namespacesOf">Reads the namespaces of an event store.</param>
    /// <returns>True if it would overlap.</returns>
    public static async Task<bool> ForNamespace(
        string eventStore,
        string @namespace,
        IEnumerable<string> existingNamespaces,
        IEnumerable<string> existingEventStores,
        Func<string, Task<IEnumerable<string>>> namespacesOf)
    {
        var names = NamesOf(eventStore, @namespace);

        if (existingNamespaces.Any(_ => !string.Equals(_, @namespace, StringComparison.OrdinalIgnoreCase) && Intersects(names, NamesOf(eventStore, _))))
        {
            return true;
        }

        foreach (var other in existingEventStores.Where(_ => !string.Equals(_, eventStore, StringComparison.OrdinalIgnoreCase)))
        {
            if (names.Contains(other, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!names.Any(_ => _.StartsWith(other + "+", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if ((await namespacesOf(other)).Any(_ => Intersects(names, NamesOf(other, _))))
            {
                return true;
            }
        }

        return false;
    }

    static string[] NamesOf(string eventStore, string @namespace) =>
    [
        $"{eventStore}+es+{@namespace}",
        string.Equals(@namespace, DefaultNamespace, StringComparison.Ordinal) ? eventStore : $"{eventStore}+{@namespace}"
    ];

    static bool Intersects(string[] first, string[] second) => first.Intersect(second, StringComparer.OrdinalIgnoreCase).Any();
}
