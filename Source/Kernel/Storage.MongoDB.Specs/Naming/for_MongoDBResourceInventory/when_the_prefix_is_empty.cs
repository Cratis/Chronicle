// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory;

public class when_the_prefix_is_empty : given.a_mongodb_resource_inventory
{
    async Task Because() => await Read();

    [Fact] void should_keep_the_server_databases_for_the_kernel() => Sentinels().ShouldContain($"mongo:{DefaultScope}|ADMIN", $"mongo:{DefaultScope}|LOCAL", $"mongo:{DefaultScope}|CONFIG");
    [Fact] void should_keep_the_cluster_database_for_the_kernel() => Sentinels().ShouldContain($"mongo:{DefaultScope}|CHRONICLE+MAIN");
}
