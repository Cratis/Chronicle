// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_Changeset.when_removing_children;

public class and_the_composite_key_is_value_equal : given.a_changeset_with_composite_child_keys
{
    void Because() => Remove(Key("tenant", "item"));

    [Fact] void should_record_one_child_removal() => _changeset.Changes.Single().ShouldBeOfExactType<ChildRemoved>();
    [Fact] void should_record_the_matching_child() => ((IDictionary<string, object?>)_changeset.Changes.OfType<ChildRemoved>().Single().Child)["name"].ShouldEqual("target");
    [Fact] void should_remove_only_the_matching_child() => Items.Select(_ => _["name"]).ShouldEqual(["other tenant", "other item"]);
    [Fact] void should_leave_the_initial_children_unchanged() => _initialChildren.Count.ShouldEqual(3);
}
