// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelSchema;

public class when_provisioning_a_missing_long_name_schema : given.a_long_name_schema_store
{
    void Establish() => _store.Exists("chronicle_long_names").Returns(false);

    async Task Because() => await _schema.Provision(_long);

    [Fact] void should_create_the_schema_explicitly() => _store.Received(1).Create("chronicle_long_names");
}
