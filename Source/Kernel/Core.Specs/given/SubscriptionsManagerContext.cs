// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;
using Orleans.TestKit;

namespace Cratis.Chronicle;

/// <summary>
/// Helpers for specifying the event store subscriptions manager.
/// </summary>
public static class SubscriptionsManagerContext
{
    /// <summary>
    /// Adds the storage the manager uses to find the event stores that subscribe to its own.
    /// </summary>
    /// <param name="silo">The <see cref="TestKitSilo"/>.</param>
    /// <returns>The <see cref="IStorage"/> that was added.</returns>
    public static IStorage AddEventStoreStorage(this TestKitSilo silo)
    {
        var storage = Substitute.For<IStorage>();
        storage.GetEventStores().Returns(_ => Task.FromResult<IEnumerable<EventStoreName>>([]));
        silo.AddService(storage);
        return storage;
    }

    /// <summary>
    /// Makes the namespaces of an event store answer that every namespace asked for exists, under the name it was asked for.
    /// </summary>
    /// <param name="namespaces">The <see cref="INamespaces"/> substitute.</param>
    /// <returns>The same <see cref="INamespaces"/>, for continuing.</returns>
    public static INamespaces HasEveryNamespace(this INamespaces namespaces)
    {
        namespaces.GetCanonical(Arg.Any<EventStoreNamespaceName>())
            .Returns(call => Task.FromResult<EventStoreNamespaceName?>(call.Arg<EventStoreNamespaceName>()));
        return namespaces;
    }
}
