// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ChangesetChildRemovals;

public class should_not_allocate : given.a_changeset
{
    long _allocated;

    void Establish()
    {
        RemoveChildFromAll();
        ChangesetChildRemovals.OnlyRemovesChildrenFromAll(_changeset);
        ChangesetChildRemovals.HasChildRemovalsFromAll(_changeset);
    }

    void Because()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            ChangesetChildRemovals.OnlyRemovesChildrenFromAll(_changeset);
            ChangesetChildRemovals.HasChildRemovalsFromAll(_changeset);
        }

        _allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact] void should_not_allocate_while_checking() => _allocated.ShouldEqual(0L);
}
