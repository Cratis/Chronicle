// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

/// <summary>
/// Two dependency injection containers in one process are ordinary - it is the shape of every web application factory
/// test - and each has its own Chronicle client. Each must get its own event store.
/// </summary>
/// <remarks>
/// The registration used to hold a static cache keyed on the namespace alone, so the first container to resolve a
/// namespace decided the event store every later container would get, built from the first container's own service
/// provider and client. The second container's client was then never asked for anything, and evicting its event
/// stores could not dislodge what this cache was still handing out.
/// </remarks>
public class when_two_containers_resolve_the_same_namespace : Specification
{
    static readonly EventStoreNamespaceName _sharedNamespace = "the-shared-namespace";

    IEventStore _firstContainersEventStore;
    IEventStore _secondContainersEventStore;
    IEventStore _fromFirstContainer;
    IEventStore _fromSecondContainer;

    void Establish()
    {
        _firstContainersEventStore = Substitute.For<IEventStore>();
        _secondContainersEventStore = Substitute.For<IEventStore>();
    }

    void Because()
    {
        _fromFirstContainer = Resolve(_firstContainersEventStore);
        _fromSecondContainer = Resolve(_secondContainersEventStore);
    }

    [Fact] void should_give_the_first_container_its_own() => _fromFirstContainer.ShouldEqual(_firstContainersEventStore);
    [Fact] void should_give_the_second_container_its_own() => _fromSecondContainer.ShouldEqual(_secondContainersEventStore);

    static IEventStore Resolve(IEventStore eventStore)
    {
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(Task.FromResult(eventStore));

        var namespaceResolver = Substitute.For<IEventStoreNamespaceResolver>();
        namespaceResolver.Resolve().Returns(_sharedNamespace);

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new ChronicleClientOptions()));
        services.AddLogging();
        services.AddCratisChronicleClient();
        services.AddSingleton(namespaceResolver);
        services.AddSingleton(client);

        using var scope = services.BuildServiceProvider().CreateScope();
        return scope.ServiceProvider.GetRequiredService<IEventStore>();
    }
}
