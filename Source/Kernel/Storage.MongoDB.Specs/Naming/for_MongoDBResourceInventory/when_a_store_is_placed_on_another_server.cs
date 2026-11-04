// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory;

public class when_a_store_is_placed_on_another_server : given.a_mongodb_resource_inventory
{
    void Establish()
    {
        _registered.AddRange(["orders", "Orders+other"]);
        _place = (kind, derived, store, _) => (store == "Orders+other" ? "other.local:27017" : DefaultScope, derived);
    }

    async Task Because() => await Read();

    [Fact] void should_use_the_placed_server() => IdentitiesOf("Orders+other").ShouldContainOnly("mongo:other.local:27017|ORDERS+OTHER+ES");
    [Fact] void should_use_the_default_server_for_the_other_store() => IdentitiesOf("orders").ShouldContainOnly($"mongo:{DefaultScope}|ORDERS+ES");
}
