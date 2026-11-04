// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// The join runs before the child removal, so the removal wins: removing first would let the join recreate the child.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_a_join_and_a_child_removal_target_the_same_child<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    IDictionary<string, object?>? _row;

    async Task Establish() => await Apply("r", 1, Set(("joinId", "G")), AddChild("c1"), AddChild("c2"));

    async Task Because()
    {
        var indexers = new ArrayIndexers([new ArrayIndexer("[children]", "id", "c1")]);
        var join = new Joined(new ExpandoObject(), "G", "joinId", indexers, [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("[children].label", null, "joined")])]) { HasKeyedFrom = true };
        await Apply("trigger", 2, join, RemoveChildFromAll("c1"));
        _row = await Row("r");
    }

    [Fact] public void should_not_contain_the_removed_child() => ((IEnumerable<object>)_row!["children"]!).Cast<IDictionary<string, object?>>().Any(child => (string?)child["id"] == "c1").ShouldBeFalse();
    [Fact] public void should_keep_the_other_child() => ChildCount(_row).ShouldEqual(1);
}
