// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes;

[Collection(SqlServerCollection.Name)]
public class and_an_object_property_is_set_to_null_on_sql_server(SqlServerFixture fixture) : Contract.and_an_object_property_is_set_to_null<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateHarness() => new() { Fixture = fixture };
}
