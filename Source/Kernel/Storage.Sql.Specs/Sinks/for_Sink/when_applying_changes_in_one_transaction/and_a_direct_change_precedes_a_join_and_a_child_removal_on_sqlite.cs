// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public class and_a_direct_change_precedes_a_join_and_a_child_removal_on_sqlite : given.a_cycle_with_failure_injection<SqlSinkHarness>
{
    IDictionary<string, object?>? _row;

    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture] };

    async Task Establish() => await Apply("r", 1, Set(("joinId", "G")), AddChild("c1"), AddChild("c2"));

    async Task Because()
    {
        var join = new Joined(new ExpandoObject(), "G", "joinId", ArrayIndexers.NoIndexers, [AddChild("c1")]) { HasKeyedFrom = true };
        await Apply("r", 2, Set(("group", "direct")), join, RemoveChildFromAll("c1"));
        _row = await Row("r");
    }

    [Fact] public void should_not_recreate_the_removed_child() => ((IEnumerable<object>)_row!["children"]!).Cast<IDictionary<string, object?>>().Any(child => (string?)child["id"] == "c1").ShouldBeFalse();
    [Fact] public void should_keep_the_other_child() => ChildCount(_row).ShouldEqual(1);
    [Fact] public void should_store_the_unrelated_direct_change() => _row!["group"].ShouldEqual("direct");
}
