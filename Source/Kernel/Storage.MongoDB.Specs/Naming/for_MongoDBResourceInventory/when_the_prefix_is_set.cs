// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory;

public class when_the_prefix_is_set : given.a_mongodb_resource_inventory
{
    void Establish() => _prefix = "run_";

    async Task Because() => await Read();

    [Fact] void should_keep_the_prefixed_cluster_database_for_the_kernel() => Sentinels().ShouldContainOnly($"mongo:{DefaultScope}|RUN_CHRONICLE+MAIN");
}
