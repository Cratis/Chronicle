// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_ReplayContextsStorage.when_getting_all;

[Collection(SqlServerCollection.Name)]
public class on_sqlserver(SqlServerFixture fixture) : for_Database.given.a_sql_server_database(fixture)
{
    EventStores.Namespaces.ReplayContexts.ReplayContextsStorage _contexts;
    IReadOnlyList<ReplayContext> _actual;
    readonly ReplayContext[] _expected = [new(new("orders", ReadModelGeneration.First), "orders", "orders-revert", DateTimeOffset.FromUnixTimeSeconds(10000)), new(new("items", ReadModelGeneration.First), "items", "items-revert", DateTimeOffset.FromUnixTimeSeconds(20000))];

    async Task Establish()
    {
        await using var store = await Database.EventStore(Store);
        _contexts = new(new(Store), new("tenant"), Database);
        foreach (var context in _expected)
        {
            await _contexts.Save(context);
        }
    }

    async Task Because() => _actual = await _contexts.GetAll();

    [Fact] void should_read_all_durable_sessions() => _actual.OrderBy(context => context.Type.Identifier.Value).ShouldEqual(_expected.OrderBy(context => context.Type.Identifier.Value));
    [Fact] async Task should_not_remove_sessions() => (await _contexts.GetAll()).Count.ShouldEqual(2);
}
