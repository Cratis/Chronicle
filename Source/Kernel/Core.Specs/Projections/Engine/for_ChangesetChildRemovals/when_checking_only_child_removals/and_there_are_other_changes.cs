// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Projections.Engine.for_ChangesetChildRemovals.when_checking_only_child_removals;

public class and_there_are_other_changes : given.a_changeset
{
    bool _only;
    bool _has;

    void Establish()
    {
        RemoveChildFromAll();
        _changeset.Add(new Removed("the-state"));
    }

    void Because()
    {
        _only = ChangesetChildRemovals.OnlyRemovesChildrenFromAll(_changeset);
        _has = ChangesetChildRemovals.HasChildRemovalsFromAll(_changeset);
    }

    [Fact] void should_not_be_only_child_removals() => _only.ShouldBeFalse();
    [Fact] void should_still_have_child_removals() => _has.ShouldBeTrue();
}
