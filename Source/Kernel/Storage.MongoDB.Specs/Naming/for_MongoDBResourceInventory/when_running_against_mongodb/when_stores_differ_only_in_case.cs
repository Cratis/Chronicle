// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory.when_running_against_mongodb;

[Collection(MongoDBCollection.Name)]
public class when_stores_differ_only_in_case(MongoDBFixture fixture) : given.a_mongodb_server_with_an_inventory(fixture)
{
    Exception? _first;
    Exception? _second;
    IReadOnlyList<ResourceUse> _uses;

    async Task Because()
    {
        _first = await TryCreate("Orders");
        _second = await TryCreate("ORDERS");
        _uses = await Read("Orders", "ORDERS");
    }

    [Fact] void should_accept_the_first_store() => _first.ShouldBeNull();
    [Fact] void should_refuse_the_second_store_on_the_server() => _second.ShouldNotBeNull();
    [Fact] void should_group_them_by_the_same_identity() => IdentitiesOf(_uses, "Orders").ShouldEqual(IdentitiesOf(_uses, "ORDERS"));
}
