// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetWriteOrder;

public class when_segmenting_at_every_join : Specification
{
    PropertiesChanged<System.Dynamic.ExpandoObject> _first;
    Joined _firstJoin;
    Joined _secondJoin;
    IReadOnlyList<IReadOnlyList<Change>>? _result;
    IReadOnlyList<IReadOnlyList<Change>>? _withoutJoin;

    void Establish()
    {
        _first = Changes.Direct("name");
        _firstJoin = Changes.Join(false, "other");
        _secondJoin = Changes.Join(false, "third");
    }

    void Because()
    {
        _result = ChangesetWriteOrder.SegmentsAtEveryJoin([_first, _firstJoin, _secondJoin]);
        _withoutJoin = ChangesetWriteOrder.SegmentsAtEveryJoin([_first]);
    }

    [Fact] void should_cut_at_every_join_even_when_nothing_overlaps() => _result!.Count.ShouldEqual(3);
    [Fact] void should_keep_the_direct_change_before_the_first_join_alone() => _result![0].ShouldContainOnly(_first);
    [Fact] void should_start_the_second_segment_with_the_first_join() => _result![1][0].ShouldEqual(_firstJoin);
    [Fact] void should_start_the_third_segment_with_the_second_join() => _result![2][0].ShouldEqual(_secondJoin);
    [Fact] void should_not_segment_a_changeset_without_a_join() => _withoutJoin.ShouldBeNull();
}
