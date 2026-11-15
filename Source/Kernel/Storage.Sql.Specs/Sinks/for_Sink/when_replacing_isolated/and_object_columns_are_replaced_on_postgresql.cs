// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_replacing_isolated;

[Collection(PostgreSqlCollection.Name)]
public class and_object_columns_are_replaced_on_postgresql(PostgreSqlFixture fixture) : when_object_columns_are_replaced
{
    protected override bool UseIsolatedReplace => true;

    protected override ISqlSinkHarness CreateHarness() => new PostgreSqlSinkHarness { Fixture = fixture };
}
