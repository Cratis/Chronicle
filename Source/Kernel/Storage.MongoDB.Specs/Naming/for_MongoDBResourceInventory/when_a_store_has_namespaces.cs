// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory;

public class when_a_store_has_namespaces : given.a_mongodb_resource_inventory
{
    void Establish()
    {
        _registered.Add("orders");
        _namespaces["orders"] = ["Default", "acme"];
    }

    async Task Because() => await Read();

    [Fact] void should_name_the_default_namespace_read_models_after_the_store() => IdentitiesOf("orders", "Default", ResourceKind.ReadModelDatabase).ShouldContainOnly($"mongo:{DefaultScope}|ORDERS");
    [Fact] void should_name_other_namespace_read_models_after_the_store_and_namespace() => IdentitiesOf("orders", "acme", ResourceKind.ReadModelDatabase).ShouldContainOnly($"mongo:{DefaultScope}|ORDERS+ACME");
    [Fact] void should_name_the_namespace_database() => IdentitiesOf("orders", "acme", ResourceKind.NamespaceDatabase).ShouldContainOnly($"mongo:{DefaultScope}|ORDERS+ES+ACME");
}
