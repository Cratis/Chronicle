// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelSchema;

public class when_provisioning_an_existing_long_name_schema : given.a_long_name_schema_store
{
    void Establish() => _store.Exists("chronicle_long_names").Returns(true);

    async Task Because() => await _schema.Provision(_long);

    [Fact] void should_not_create_the_schema_again() => _store.DidNotReceiveWithAnyArgs().Create(default!);
}
