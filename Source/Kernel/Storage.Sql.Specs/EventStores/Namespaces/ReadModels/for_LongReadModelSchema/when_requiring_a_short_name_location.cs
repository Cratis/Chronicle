// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_LongReadModelSchema;

public class when_requiring_a_short_name_location : given.a_long_name_schema_store
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(() => _schema.Require(_short));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_not_touch_the_store() => _store.ReceivedCalls().ShouldBeEmpty();
}
