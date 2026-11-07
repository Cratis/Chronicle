// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions.given;

public class a_host_resolving_the_event_store : Specification
{
    protected ServiceProvider _provider;
    protected IEventStoreNamespaceResolver _namespaceResolver;
    protected IChronicleClient _client;

    void Establish()
    {
        _client = Substitute.For<IChronicleClient>();
        _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(call =>
        {
            var store = Substitute.For<IEventStore>();
            store.Namespace.Returns(call.Arg<EventStoreNamespaceName>());
            return Task.FromResult(store);
        });
        _namespaceResolver = Substitute.For<IEventStoreNamespaceResolver>();
        _namespaceResolver.Resolve().Returns((EventStoreNamespaceName)"namespace-y");

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new ChronicleClientOptions { EventStore = "store" }));
        services.AddSingleton(Options.Create(new ChronicleOptions()));
        services.AddLogging();
        services.AddCratisChronicleClient();
        services.AddSingleton(_client);
        services.AddSingleton(_namespaceResolver);
        _provider = services.BuildServiceProvider();
    }

    void Destroy() => _provider.Dispose();

    protected EventStoreNamespaceName ResolveNamespace()
    {
        using var scope = _provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IEventStore>().Namespace;
    }
}
