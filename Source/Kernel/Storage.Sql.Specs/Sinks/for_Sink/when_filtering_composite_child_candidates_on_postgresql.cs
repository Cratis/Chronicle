// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

[Collection(PostgreSqlCollection.Name)]
public class when_filtering_composite_child_candidates_on_postgresql(PostgreSqlFixture fixture) : when_filtering_composite_child_candidates
{
    protected override ISqlSinkHarness CreateHarness(SqlReadCapture capture) => new PostgreSqlSinkHarness { Interceptors = [capture], Fixture = fixture };
}
