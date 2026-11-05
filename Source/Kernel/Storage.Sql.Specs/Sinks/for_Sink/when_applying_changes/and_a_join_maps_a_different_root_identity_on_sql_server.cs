// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_applying_changes;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes;

[Collection(SqlServerCollection.Name)]
public class and_a_join_maps_a_different_root_identity_on_sql_server(SqlServerFixture fixture) : Contract.and_a_join_maps_a_different_root_identity<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateHarness() => new() { Fixture = fixture };
}
