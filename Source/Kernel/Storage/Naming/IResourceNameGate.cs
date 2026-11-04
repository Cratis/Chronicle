// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Defines the authoritative check of event store names and namespace names before storage is opened or created.
/// </summary>
public interface IResourceNameGate
{
    /// <summary>
    /// Check that an event store is not quarantined and that the names have been checked. Does not apply the rule.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <exception cref="ResourceNamesNotReady">The names have not been checked yet.</exception>
    /// <exception cref="ResourceQuarantined">The event store is quarantined.</exception>
    void EnsureAvailable(EventStoreName eventStore);

    /// <summary>
    /// Check that an event store and namespace are not quarantined and that the names have been checked. Does not apply the rule.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <exception cref="ResourceNamesNotReady">The names have not been checked yet.</exception>
    /// <exception cref="ResourceQuarantined">The event store or namespace is quarantined.</exception>
    void EnsureAvailable(EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Check that the storage of an event store can be opened: the rule, unless the name is registered, then availability.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <exception cref="InvalidResourceName">The name breaks the rule and is not registered.</exception>
    /// <exception cref="ResourceNamesNotReady">The names have not been checked yet.</exception>
    /// <exception cref="ResourceQuarantined">The event store is quarantined.</exception>
    void EnsureCanOpen(EventStoreName eventStore);

    /// <summary>
    /// Check that the storage of a namespace can be opened: the rule, unless the names are registered, then availability.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <exception cref="InvalidResourceName">A name breaks the rule and is not registered.</exception>
    /// <exception cref="ResourceNamesNotReady">The names have not been checked yet.</exception>
    /// <exception cref="ResourceQuarantined">The event store or namespace is quarantined.</exception>
    void EnsureCanOpen(EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Record that an event store exists, so that it stays usable even if its name breaks the rule.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    void MarkRegistered(EventStoreName eventStore);

    /// <summary>
    /// Record that a namespace exists, so that it stays usable even if its name breaks the rule.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    void MarkRegistered(EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Check that a new event store or namespace can be created, and reserve it.
    /// </summary>
    /// <param name="creation">The <see cref="ResourceCreation"/>.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidResourceName">A name breaks the rule.</exception>
    /// <exception cref="ResourceNameConflict">The name is a case variant of an existing name, or its storage would overlap.</exception>
    Task EnsureCanCreate(ResourceCreation creation);
}
