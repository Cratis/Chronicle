// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory;

public class when_a_store_is_placed_on_a_named_database : given.a_mongodb_resource_inventory
{
    void Establish()
    {
        _registered.Add("orders");
        _place = (kind, derived, _, _) => (DefaultScope, kind == StorageConnectionKind.EventStore ? "Platform-Orders" : derived);
    }

    async Task Because() => await Read();

    [Fact] void should_use_the_placed_database() => IdentitiesOf("orders").ShouldContainOnly($"mongo:{DefaultScope}|PLATFORM-ORDERS");
}
