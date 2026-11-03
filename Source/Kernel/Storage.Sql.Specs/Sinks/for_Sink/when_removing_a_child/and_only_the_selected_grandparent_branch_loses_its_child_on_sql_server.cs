// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.when_removing_a_child;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_removing_a_child;

[Collection(SqlServerCollection.Name)]
public class and_only_the_selected_grandparent_branch_loses_its_child_on_sql_server(SqlServerFixture fixture) : Contract.and_only_the_selected_grandparent_branch_loses_its_child<SqlServerSinkHarness>
{
    protected override SqlServerSinkHarness CreateHarness() => new() { Fixture = fixture };
}
