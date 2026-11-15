// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Decides which namespaces of the source event store an event store subscription observes.
/// </summary>
public static class SubscriptionObservation
{
    /// <summary>
    /// Gets whether the subscription observes the namespace. A name containing a comma is never observed, so a malformed
    /// namespace is not mirrored into the source event store. An empty list of source namespaces observes every other namespace.
    /// </summary>
    /// <param name="definition">The <see cref="EventStoreSubscriptionDefinition"/>.</param>
    /// <param name="namespace">The namespace of the target event store, which is mirrored under the same name in the source.</param>
    /// <returns>True if observed; false otherwise.</returns>
    public static bool Observes(EventStoreSubscriptionDefinition definition, EventStoreNamespaceName @namespace)
    {
        if (NamespaceNameRules.ContainsComma(@namespace.Value))
        {
            return false;
        }

        var restriction = definition.SourceNamespaces.ToArray();
        return restriction.Length == 0
            || restriction.Any(name => name.Value.Equals(@namespace.Value, StringComparison.OrdinalIgnoreCase));
    }
}
