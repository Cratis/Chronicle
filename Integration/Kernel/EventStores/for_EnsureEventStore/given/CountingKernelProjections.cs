// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Projections.Kernel;

using KernelEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;

namespace Cratis.Chronicle.Kernel.Integration.EventStores.for_EnsureEventStore.given;

public class CountingKernelProjections(IKernelProjections inner) : IKernelProjections
{
    readonly ConcurrentDictionary<KernelEventStoreName, int> _calls = new();

    public int CallsFor(KernelEventStoreName store) => _calls.GetValueOrDefault(store);

    public Task DiscoverAndRegister(KernelEventStoreName eventStore)
    {
        _calls.AddOrUpdate(eventStore, 1, (_, count) => count + 1);
        return inner.DiscoverAndRegister(eventStore);
    }
}
