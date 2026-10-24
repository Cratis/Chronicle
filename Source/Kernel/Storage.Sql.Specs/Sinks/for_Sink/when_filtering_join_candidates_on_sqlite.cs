// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink;

public class when_filtering_join_candidates_on_sqlite : when_filtering_join_candidates
{
    protected override ISqlSinkHarness CreateHarness(SqlReadCapture capture) => new SqlSinkHarness { Interceptors = [capture] };
}
