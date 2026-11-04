// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory;

public class when_names_differ_only_by_normalization : given.a_mongodb_resource_inventory
{
    void Establish() => _registered.AddRange(["caf\u00e9", "cafe\u0301"]);

    async Task Because() => await Read();

    [Fact] void should_give_distinct_identities() => IdentitiesOf("caf\u00e9").ShouldNotEqual(IdentitiesOf("cafe\u0301"));
}
