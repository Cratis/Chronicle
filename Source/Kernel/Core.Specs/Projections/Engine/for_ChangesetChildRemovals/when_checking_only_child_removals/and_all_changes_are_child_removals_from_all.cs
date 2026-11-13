// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ChangesetChildRemovals.when_checking_only_child_removals;

public class and_all_changes_are_child_removals_from_all : given.a_changeset
{
    bool _only;
    bool _has;

    void Establish()
    {
        RemoveChildFromAll();
        RemoveChildFromAll();
    }

    void Because()
    {
        _only = ChangesetChildRemovals.OnlyRemovesChildrenFromAll(_changeset);
        _has = ChangesetChildRemovals.HasChildRemovalsFromAll(_changeset);
    }

    [Fact] void should_only_remove_children() => _only.ShouldBeTrue();
    [Fact] void should_have_child_removals() => _has.ShouldBeTrue();
}
