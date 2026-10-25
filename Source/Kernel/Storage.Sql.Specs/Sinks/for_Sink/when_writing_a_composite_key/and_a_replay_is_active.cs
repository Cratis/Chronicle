// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_writing_a_composite_key;

public class and_a_replay_is_active : given.a_composite_key_sink
{
    string _primary;
    string _replay;
    bool _leaked;

    protected override ISqlSinkHarness CreateHarness() => new SqlSinkHarness();

    async Task Because()
    {
        await Write("a", "b", "primary");
        await _sink.BeginReplay(new ReplayContext(new ReadModelType("composite", ReadModelGeneration.First), Table, $"{Table}-revert", DateTimeOffset.UtcNow));
        await Write("c", "d", "replay");
        await using var context = CreateContext();
        _primary = await context.Database.SqlQuery<string>($"SELECT __compositeKey AS Value FROM composite_shadows").SingleAsync();
        _replay = await context.Database.SqlQuery<string>($"SELECT __compositeKey AS Value FROM \"replay-composite_shadows\"").SingleAsync();
        _leaked = ((IDictionary<string, object?>)(await _sink.GetInstances()).Instances.Single()).ContainsKey("__compositeKey");
    }

    [Fact] void should_stamp_the_active_replay_table() => _replay.ShouldEqual("~k1:1:c1:d");
    [Fact] void should_leave_the_primary_stamp_unchanged() => _primary.ShouldEqual("~k1:1:a1:b");
    [Fact] void should_never_materialize_the_shadow() => _leaked.ShouldBeFalse();
}
