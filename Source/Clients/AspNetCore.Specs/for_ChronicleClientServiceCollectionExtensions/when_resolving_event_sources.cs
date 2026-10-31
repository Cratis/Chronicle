// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

/// <summary>
/// Event source definitions are a per-store service like the event log and the read models, so code that declares an
/// event source - an Arc command with <c language="csharp">[EventSource&lt;T&gt;]</c>, an event-sourced aggregate root - can take them
/// from the container. Without the registration such code failed before it ran in every hosted application.
/// </summary>
public class when_resolving_event_sources : Specification
{
    IEventSources _eventSources;
    IEventSources _resolved;
    IChronicleClient _client;

    void Establish()
    {
        _eventSources = Substitute.For<IEventSources>();
        var eventStore = Substitute.For<IEventStore>();
        eventStore.EventSources.Returns(_eventSources);

        _client = Substitute.For<IChronicleClient>();
        _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(Task.FromResult(eventStore));
    }

    void Because()
    {
        var namespaceResolver = Substitute.For<IEventStoreNamespaceResolver>();
        namespaceResolver.Resolve().Returns(new EventStoreNamespaceName($"ns-{Guid.NewGuid():N}"));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCratisChronicleClient();
        services.AddSingleton(namespaceResolver);
        services.AddSingleton(_client);

        using var scope = services.BuildServiceProvider().CreateScope();
        _resolved = scope.ServiceProvider.GetRequiredService<IEventSources>();
    }

    [Fact] void should_resolve_the_event_stores_event_sources() => _resolved.ShouldEqual(_eventSources);
}
