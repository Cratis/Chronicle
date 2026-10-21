// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

[Collection(SqlServerCollection.Name)]
public class when_filtering_join_candidates_on_sql_server(SqlServerFixture fixture) : when_filtering_join_candidates
{
    protected override ISqlSinkHarness CreateHarness(SqlReadCapture capture) => new SqlServerSinkHarness { Interceptors = [capture], Fixture = fixture };
}
