// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_Changeset.when_removing_children;

public class and_only_the_first_key_part_matches : given.a_changeset_with_composite_child_keys
{
    void Because() => Remove(Key("tenant", "missing"));

    [Fact] void should_not_record_a_removal() => _changeset.Changes.ShouldBeEmpty();
    [Fact] void should_keep_all_children() => Items.Count().ShouldEqual(3);
}
