// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory;

public class when_reading : given.a_mongodb_resource_inventory
{
    static readonly string[] _allowed = ["ListDatabaseNamesAsync", "get_Client", "GetCollection", "FindAsync", "Placement", "GetDatabase", "GetEventStores"];

    void Establish()
    {
        _registered.Add("orders");
        _namespaces["orders"] = ["acme"];
    }

    async Task Because() => await Read();

    [Fact] void should_only_read_from_the_client() => _client.ReceivedCalls().All(Reads).ShouldBeTrue();
    [Fact] void should_only_read_from_the_databases() => _databases.SelectMany(_ => _.ReceivedCalls()).All(Reads).ShouldBeTrue();
    [Fact] void should_only_read_from_the_collections() => _collections.SelectMany(_ => _.ReceivedCalls()).All(Reads).ShouldBeTrue();

    static bool Reads(ICall call) => _allowed.Contains(call.GetMethodInfo().Name);
}
