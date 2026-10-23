// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Storage.Sql.for_IntegrationTimings;

public class when_observing_a_failed_command : Specification
{
    readonly List<string> _lines = [];
    JsonElement _record;

    void Because()
    {
        var command = Guid.NewGuid();
        using (var observer = CommandEvents.NewObserver(_lines, out var timings))
        {
            using var context = observer.Track(CommandEvents.NewContext());
            observer.OnNext(CommandEvents.Executing(context, command));
            observer.OnNext(CommandEvents.Failed(context, command, 5));
            observer.Dispose();
            timings.Complete();
        }
        _record = JsonSerializer.Deserialize<JsonElement>(_lines[0][(_lines[0].IndexOf(' ') + 1)..]);
    }

    [Fact] void should_pair_the_failed_command() => _record.GetProperty("count").GetInt32().ShouldEqual(1);
    [Fact] void should_count_failure_once() => _record.GetProperty("failures").GetInt32().ShouldEqual(1);
    [Fact] void should_label_command_time_as_an_aggregate() => _record.GetProperty("accounting").GetString().ShouldEqual("aggregate-command-duration");
    [Fact] void should_not_output_sql() => _lines.Any(line => line.Contains(CommandEvents.PrivateSql, StringComparison.Ordinal)).ShouldBeFalse();
}
