// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Decides which namespaces of the source event store an event store subscription observes.
/// </summary>
public static class SubscriptionObservation
{
    /// <summary>
    /// Gets whether the subscription can observe the namespace of the target event store. A name containing a comma is never
    /// observed, so a malformed namespace is not mirrored into the source event store. Every other namespace is observed, but only
    /// where the source event store already has a namespace of that name: a subscription never creates one.
    /// </summary>
    /// <param name="namespace">The namespace of the target event store, which is observed under the same name in the source when the source has it.</param>
    /// <returns>True if observed; false otherwise.</returns>
    public static bool Observes(EventStoreNamespaceName @namespace)
    {
        return !NamespaceNameRules.ContainsComma(@namespace.Value);
    }
}
