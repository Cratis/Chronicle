// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory;

public class when_stores_differ_only_in_case : given.a_mongodb_resource_inventory
{
    void Establish() => _registered.AddRange(["Orders", "ORDERS"]);

    async Task Because() => await Read();

    [Fact] void should_give_both_the_same_identity() => IdentitiesOf("Orders").ShouldEqual(IdentitiesOf("ORDERS"));
}
