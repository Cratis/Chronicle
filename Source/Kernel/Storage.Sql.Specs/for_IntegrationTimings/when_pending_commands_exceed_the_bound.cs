// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Storage.Sql.for_IntegrationTimings;

public class when_pending_commands_exceed_the_bound : Specification
{
    readonly List<string> _lines = [];
    JsonElement _record;

    void Because()
    {
        using var observer = CommandEvents.NewObserver(_lines, out var timings);
        using var context = observer.Track(CommandEvents.NewContext());
        for (var index = 0; index <= 4096; index++) observer.OnNext(CommandEvents.Executing(context, Guid.NewGuid()));
        var command = Guid.NewGuid();
        observer.OnNext(CommandEvents.Executing(context, command));
        observer.Dispose();
        timings.Complete();
        _record = JsonSerializer.Deserialize<JsonElement>(_lines[0][(_lines[0].IndexOf(' ') + 1)..]);
    }

    [Fact] void should_mark_the_observation_partial() => _record.GetProperty("completeness").GetString().ShouldEqual("partial");
    [Fact] void should_not_report_commands_that_never_finished() => _record.GetProperty("count").GetInt32().ShouldEqual(0);
}
