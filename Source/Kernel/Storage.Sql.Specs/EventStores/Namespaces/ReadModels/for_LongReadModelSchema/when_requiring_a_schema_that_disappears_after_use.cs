// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelSchema;

public class when_requiring_a_schema_that_disappears_after_use : given.a_long_name_schema_store
{
    Exception _first;
    Exception _second;

    void Establish() => _store.Exists("chronicle_long_names").Returns(true, false);

    async Task Because()
    {
        _first = await Catch.Exception(() => _schema.Require(_long));
        _second = await Catch.Exception(() => _schema.Require(_long));
    }

    [Fact] void should_succeed_while_it_exists() => _first.ShouldBeNull();
    [Fact] void should_not_conceal_the_missing_schema_behind_a_cache() => _second.ShouldBeOfExactType<LongNameSchemaMissing>();
    [Fact] void should_never_create_the_schema() => _store.DidNotReceiveWithAnyArgs().Create(default!);
}
