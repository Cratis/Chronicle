// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.for_ChronicleClient;

/// <summary>
/// An event store is built once and kept, so that a blocking connect happens once rather than once per caller. What
/// keeps a completed construction keeps a faulted one just as readily.
/// </summary>
/// <remarks>
/// Without evicting the faulted entry, a single inability to reach the kernel at the first call would be permanent
/// for that event store: every later call replays the same exception for the lifetime of the client, and nothing
/// short of a new client could clear it. A kernel that is briefly unreachable while a host starts is ordinary.
/// </remarks>
public class when_getting_an_event_store_after_a_failed_first_attempt : Specification
{
    ChronicleClient _client;
    Exception _firstAttempt;
    IEventStore _secondAttempt;

    void Establish()
    {
        var attempts = 0;
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(Substitute.For<IConnectionLifecycle>());
        ((IChronicleServicesAccessor)connection).Services.Returns(Substitute.For<IServices>());

        // Connecting is the last thing constructing an event store does, so failing it fails the construction the
        // same way an unreachable kernel would.
        connection.Connect().Returns(_ => ++attempts == 1
            ? Task.FromException(new InvalidOperationException("the kernel was not reachable"))
            : Task.CompletedTask);

        _client = new ChronicleClient(
            connection,
            new ChronicleOptions { AutoDiscoverAndRegister = false });
    }

    async Task Because()
    {
        _firstAttempt = await Catch.Exception(() => _client.GetEventStore("the-store", "the-namespace"));
        _secondAttempt = await _client.GetEventStore("the-store", "the-namespace");
    }

    [Fact] void should_surface_the_first_failure() => _firstAttempt.ShouldNotBeNull();
    [Fact] void should_build_one_on_the_next_attempt() => _secondAttempt.ShouldNotBeNull();
}
