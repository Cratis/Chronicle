// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Observation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions.given;

public class a_host_resolving_the_event_store : Specification
{
    protected ServiceProvider _provider;
    protected IEventStoreNamespaceResolver _namespaceResolver;

    void Establish()
    {
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(call =>
        {
            var store = Substitute.For<IEventStore>();
            store.Namespace.Returns(call.Arg<EventStoreNamespaceName>());
            return Task.FromResult(store);
        });
        _namespaceResolver = Substitute.For<IEventStoreNamespaceResolver>();
        _namespaceResolver.Resolve().Returns((EventStoreNamespaceName)"namespace-y");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCratisChronicleClient();
        services.AddSingleton(_namespaceResolver);
        services.AddSingleton(client);
        _provider = services.BuildServiceProvider();
    }

    void Destroy() => _provider.Dispose();

    protected EventStoreNamespaceName ResolveNamespaceDuringDeliveryFor(EventStoreName eventStore)
    {
        // The delivery entry point belongs to the client assembly, so it is driven through the context it publishes.
        var current = typeof(ObserverDeliveryContext).GetProperty(nameof(ObserverDeliveryContext.Current), BindingFlags.Public | BindingFlags.Static)!;
        var previous = ObserverDeliveryContext.Current;
        var setter = current.GetSetMethod(true)!;
        setter.Invoke(null, [new ObserverDeliveryContext(eventStore, "namespace-x", new("observer"), ObserverDeliveryKind.Reactor, typeof(object))]);
        try
        {
            using var scope = _provider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IEventStore>().Namespace;
        }
        finally
        {
            setter.Invoke(null, [previous]);
        }
    }
}
