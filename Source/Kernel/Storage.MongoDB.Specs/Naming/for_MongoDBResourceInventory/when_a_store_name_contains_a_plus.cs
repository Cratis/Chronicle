// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory;

public class when_a_store_name_contains_a_plus : given.a_mongodb_resource_inventory
{
    void Establish()
    {
        _registered.Add("a+b");
        _physical.Add("c+d+es");
    }

    async Task Because() => await Read();

    [Fact] void should_include_the_registered_store() => IdentitiesOf("a+b").ShouldContainOnly($"mongo:{DefaultScope}|A+B+ES");
    [Fact] void should_include_the_discovered_store() => IdentitiesOf("c+d").ShouldContainOnly($"mongo:{DefaultScope}|C+D+ES");
}
