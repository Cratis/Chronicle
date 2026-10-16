// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Defines a system that removes a namespace from an event store.
/// </summary>
public interface INamespaceRemover
{
    /// <summary>
    /// Remove a namespace: unsubscribe and remove its observers (and with them their reminders), drop the data
    /// stored for it and forget it. Removing a namespace that does not exist does nothing, and a removal that was
    /// interrupted can be repeated.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the namespace belongs to.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to remove.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="NamespaceCannotBeRemoved">The namespace is the default namespace.</exception>
    Task Remove(EventStoreName eventStore, EventStoreNamespaceName @namespace);
}
