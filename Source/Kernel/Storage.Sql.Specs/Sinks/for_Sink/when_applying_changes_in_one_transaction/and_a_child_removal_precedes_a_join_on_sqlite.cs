// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public class and_a_child_removal_precedes_a_join_on_sqlite : given.a_cycle_with_failure_injection<SqlSinkHarness>
{
    IDictionary<string, object?>? _row;

    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture] };

    async Task Establish() => await Apply("r", 1, Set(("joinId", "G")), AddChild("c1"), AddChild("c2"));

    async Task Because()
    {
        var join = new Joined(new ExpandoObject(), "G", "joinId", ArrayIndexers.NoIndexers, [AddChild("c1")]);
        await Apply("r", 2, RemoveChildFromAll("c1"), join);
        _row = await Row("r");
    }

    [Fact] public void should_recreate_the_removed_child() => ((IEnumerable<object>)_row!["children"]!).Cast<IDictionary<string, object?>>().Any(child => (string?)child["id"] == "c1").ShouldBeTrue();
    [Fact] public void should_keep_both_children() => ChildCount(_row).ShouldEqual(2);
}
