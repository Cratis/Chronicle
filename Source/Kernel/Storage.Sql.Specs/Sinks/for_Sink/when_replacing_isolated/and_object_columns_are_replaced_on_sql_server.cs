// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_replacing_isolated;

[Collection(SqlServerCollection.Name)]
public class and_object_columns_are_replaced_on_sql_server(SqlServerFixture fixture) : when_object_columns_are_replaced
{
    protected override bool UseIsolatedReplace => true;

    protected override ISqlSinkHarness CreateHarness() => new SqlServerSinkHarness { Fixture = fixture };
}
