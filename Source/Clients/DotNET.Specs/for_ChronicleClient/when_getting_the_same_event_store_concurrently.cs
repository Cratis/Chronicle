// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.for_ChronicleClient;

/// <summary>
/// Constructing an event store discovers every client artifact and connects, so it has to happen once per event store
/// and namespace no matter how many callers ask at once.
/// </summary>
/// <remarks>
/// The cache checked and then added without holding anything in between, so two concurrent callers each built a whole
/// event store, each ran discovery, and the second overwrote the first - handing one of them a store that was no
/// longer the cached one, while the connection lifecycle kept registration handlers for both.
/// </remarks>
public class when_getting_the_same_event_store_concurrently : Specification
{
    ChronicleClient _client;
    IEventStore[] _sameKey;
    IEventStore _anotherNamespace;
    IEventStore _anotherEventStore;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(Substitute.For<IConnectionLifecycle>());
        ((IChronicleServicesAccessor)connection).Services.Returns(Substitute.For<IServices>());

        _client = new ChronicleClient(
            connection,
            new ChronicleOptions { AutoDiscoverAndRegister = false });
    }

    async Task Because()
    {
        _sameKey = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => Task.Run(() => _client.GetEventStore("the-store", "the-namespace"))));
        _anotherNamespace = await _client.GetEventStore("the-store", "another-namespace");
        _anotherEventStore = await _client.GetEventStore("another-store", "the-namespace");
    }

    [Fact] void should_hand_every_caller_the_same_instance() => _sameKey.Distinct().Count().ShouldEqual(1);
    [Fact] void should_hand_another_namespace_a_different_one() => _anotherNamespace.ShouldNotEqual(_sameKey[0]);
    [Fact] void should_hand_another_event_store_a_different_one() => _anotherEventStore.ShouldNotEqual(_sameKey[0]);
}
