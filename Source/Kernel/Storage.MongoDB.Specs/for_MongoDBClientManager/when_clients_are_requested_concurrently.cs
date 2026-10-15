// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBClientManager;

public class when_clients_are_requested_concurrently : Specification
{
    const int CallerCount = 16;
    IMongoDBClientFactory _factory;
    MongoDBClientManager _manager;
    IMongoClient[] _clients;
    Barrier _start;

    void Establish()
    {
        _start = new Barrier(CallerCount);
        _factory = Substitute.For<IMongoDBClientFactory>();
        _factory.Create(Arg.Any<MongoClientSettings>()).Returns(_ =>
        {
            // Widen the interleaving window on concurrent first access.
            Thread.Yield();
            return Substitute.For<IMongoClient>();
        });
        _manager = new MongoDBClientManager(_factory);
    }

    async Task Because() => _clients = await Task.WhenAll(Enumerable.Range(0, CallerCount).Select(_ => Task.Factory.StartNew(
        () =>
        {
            _start.SignalAndWait(TimeSpan.FromSeconds(10)).ShouldBeTrue();
            return _manager.GetClientFor(new MongoClientSettings());
        },
        CancellationToken.None,
        TaskCreationOptions.LongRunning,
        TaskScheduler.Default)));

    void Destroy() => _start.Dispose();

    [Fact] void should_create_only_one_client() => _factory.Received(1).Create(Arg.Any<MongoClientSettings>());
    [Fact] void should_share_the_client_between_callers() => _clients.All(client => ReferenceEquals(client, _clients[0])).ShouldBeTrue();
}
