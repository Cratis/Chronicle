// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Events.for_TestingArtifactCatalog;

public class when_disposing_a_store : Specification
{
    WeakReference _store;
    WeakReference _services;

    void Because()
    {
        (_store, _services) = CreateAndDispose();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Fact] void should_not_retain_the_disposed_store() => _store.IsAlive.ShouldBeFalse();
    [Fact] void should_not_retain_its_borrowed_service_provider() => _services.IsAlive.ShouldBeFalse();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (WeakReference Store, WeakReference Services) CreateAndDispose()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        using var store = new EventStoreForTesting(services, new InMemoryClientArtifactsProvider());
        _ = store.Reactors;
        store.Dispose();
        store.Dispose();
        return (new(store), new(services));
    }
}
