// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Represents the immutable result of the last check of the names in use, published for the request paths.
/// </summary>
public interface IResourceNameSnapshot
{
    /// <summary>
    /// Gets whether the names of the installation have been checked.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Gets whether any event store or namespace is quarantined.
    /// </summary>
    bool HasQuarantine { get; }

    /// <summary>
    /// Check the availability of an event store, and optionally one of its namespaces.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The optional name of the namespace.</param>
    /// <returns>The <see cref="ResourceNameStatus"/>.</returns>
    ResourceNameStatus Check(string eventStore, string? @namespace = null);

    /// <summary>
    /// Check whether an event store is quarantined, in whole or for any of its namespaces.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <returns>True if the event store itself is quarantined.</returns>
    bool IsQuarantined(EventStoreName eventStore);

    /// <summary>
    /// Check whether a namespace, or the event store it belongs to, is quarantined.
    /// </summary>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The name of the namespace.</param>
    /// <returns>True if unavailable because of quarantine.</returns>
    bool IsQuarantined(EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
