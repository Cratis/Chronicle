// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetWriteOrder;

public class when_the_changeset_has_no_keyed_join_and_is_checked_for_allocations : Specification
{
    List<Change> _changes;
    IReadOnlyList<IReadOnlyList<Change>>? _result;
    long _allocated;

    void Establish() => _changes = [Changes.Direct("count"), Changes.Direct("name")];

    void Because()
    {
        for (var warmUp = 0; warmUp < 10; warmUp++)
        {
            ChangesetWriteOrder.SegmentsFor(_changes);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        _result = ChangesetWriteOrder.SegmentsFor(_changes);
        _allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact] void should_use_the_single_pass() => _result.ShouldBeNull();
    [Fact] void should_not_allocate() => _allocated.ShouldEqual(0L);
}
