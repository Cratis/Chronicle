// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_ChronicleClientServiceCollectionExtensions;

/// <summary>
/// The resolved event store is memoized per namespace so that a blocking connect happens once rather than once per
/// scope. What memoizes it memoizes a failure just as readily - so a single inability to reach the kernel at the
/// first resolution used to be permanent for that namespace: every later resolution replayed the same exception
/// for the lifetime of the process, with nothing short of a restart able to clear it.
/// </summary>
/// <remarks>
/// A kernel that is briefly unreachable while a host is starting is ordinary, and surviving it is the whole point
/// of resolving on first use rather than at startup.
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
