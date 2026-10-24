// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Sql.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_IntegrationTimings;

public class when_observing_scoped_commands : Specification
{
    readonly List<string> _lines = [];
    JsonElement _record;

    void Because()
    {
        using var observer = CommandEvents.NewObserver(_lines, out var timings);
        using var tracked = observer.Track(CommandEvents.NewContext());
        using var untracked = CommandEvents.NewContext();
        Run(observer, tracked);
        Run(observer, untracked);
        observer.Dispose();
        Run(observer, tracked);
        observer.Dispose();
        timings.Complete();
        _record = JsonSerializer.Deserialize<JsonElement>(_lines[0][(_lines[0].IndexOf(' ') + 1)..]);
    }

    static void Run(SqlCommandTimings observer, DbContext context)
    {
        var command = Guid.NewGuid();
        observer.OnNext(CommandEvents.Executing(context, command));
        observer.OnNext(CommandEvents.Executed(context, command, 2));
    }

    [Fact] void should_count_only_the_registered_live_context() => _record.GetProperty("count").GetInt32().ShouldEqual(1);
    [Fact] void should_record_no_failures() => _record.GetProperty("failures").GetInt32().ShouldEqual(0);
    [Fact] void should_report_complete_pairs() => _record.GetProperty("completeness").GetString().ShouldEqual("complete");
    [Fact] void should_emit_only_one_aggregate_and_summary() => _lines.Count.ShouldEqual(2);
}
