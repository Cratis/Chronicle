// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelSchema;

public class when_requiring_a_missing_long_name_schema : given.a_long_name_schema_store
{
    Exception _exception;

    void Establish() => _store.Exists("chronicle_long_names").Returns(false);

    async Task Because() => _exception = await Catch.Exception(() => _schema.Require(_long));

    [Fact] void should_throw_long_name_schema_missing() => _exception.ShouldBeOfExactType<LongNameSchemaMissing>();
    [Fact] void should_name_the_schema() => ((LongNameSchemaMissing)_exception).Schema.ShouldEqual("chronicle_long_names");
    [Fact] void should_recommend_the_migration_command() => _exception.Message.ShouldContain("chronicle --migrate-only");
    [Fact] void should_never_create_the_schema() => _store.DidNotReceiveWithAnyArgs().Create(default!);
}
