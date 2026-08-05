// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

/// <summary>
/// Resolving an event store that could not be reached must not poison the registration: the next resolution has to
/// try again rather than replay the first failure.
/// </summary>
/// <remarks>
/// This layer holds no cache of its own - the client keys its event stores on both name and namespace and evicts a
/// faulted one, and this registration delegates to it. A second cache here once memoized the failure itself, and
/// being static it also handed every container in the process the same event store. What is pinned here is that the
/// registration keeps delegating rather than growing that cache back.
/// </remarks>
public class when_resolving_the_event_store_after_a_failed_first_attempt : Specification
{
    IServiceProvider _serviceProvider;
    IEventStore _eventStore;
    Exception _firstAttempt;
    IEventStore _secondAttempt;

    void Establish()
    {
        _eventStore = Substitute.For<IEventStore>();

        var attempts = 0;
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(_ => ++attempts == 1
                ? throw new InvalidOperationException("the kernel was not reachable")
                : Task.FromResult(_eventStore));

        // The namespace keys the memo, and the memo is process-wide - so this specification takes a namespace of
        // its own rather than sharing the default one with every other specification in the run.
        var namespaceResolver = Substitute.For<IEventStoreNamespaceResolver>();
        namespaceResolver.Resolve().Returns(new EventStoreNamespaceName($"ns-{Guid.NewGuid():N}"));

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new ChronicleClientOptions()));
        services.AddLogging();
        services.AddCratisChronicleClient();

        // Registered after the extension, because the extension registers its own of both and the last
        // registration is the one that resolves.
        services.AddSingleton(namespaceResolver);
        services.AddSingleton(client);

        _serviceProvider = services.BuildServiceProvider();
    }

    void Because()
    {
        _firstAttempt = Catch.Exception(() =>
        {
            using var scope = _serviceProvider.CreateScope();
            scope.ServiceProvider.GetRequiredService<IEventStore>();
        });

        using var secondScope = _serviceProvider.CreateScope();
        _secondAttempt = secondScope.ServiceProvider.GetRequiredService<IEventStore>();
    }

    [Fact] void should_surface_the_first_failure() => _firstAttempt.ShouldNotBeNull();
    [Fact] void should_resolve_on_the_next_attempt() => _secondAttempt.ShouldEqual(_eventStore);
}
