// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    bool _namespaceEnsured;

    /// <summary>
    /// Brings the observed namespace into being when an explicit subscription names it.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// An application that connects to its own event store with a new tenant namespace and registers its observers there
    /// is the explicit act that creates the namespace. Activating an event sequence never does: a restart waking persisted
    /// observers, or a read, must not bring back a namespace that was removed. Automatic reconciliation and recovery do not
    /// call this either.
    /// </remarks>
    async Task EnsureObservedNamespaceExists()
    {
        if (_namespaceEnsured) return;
        if (_observerKey.EventStore == EventStoreName.System && _observerKey.Namespace == EventStoreNamespaceName.Default)
        {
            _namespaceEnsured = true;
            return;
        }

        var namespaces = GrainFactory.GetGrain<INamespaces>(_observerKey.EventStore);
        if (await namespaces.GetCanonical(_observerKey.Namespace) is null)
        {
            await namespaces.EnsureCanonical(_observerKey.Namespace);
        }

        _namespaceEnsured = true;
    }
}
