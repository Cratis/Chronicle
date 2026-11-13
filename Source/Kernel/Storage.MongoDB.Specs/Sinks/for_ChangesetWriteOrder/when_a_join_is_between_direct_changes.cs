// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetWriteOrder;

public class when_a_join_is_between_direct_changes : Specification
{
    Change[] _changes;
    PropertiesChanged<ExpandoObject> _first;
    Joined _join;
    PropertiesChanged<ExpandoObject> _last;
    IReadOnlyList<IReadOnlyList<Change>>? _result;

    void Establish()
    {
        _first = Changes.Direct("count", "x");
        _join = Changes.Join(true, "count");
        _last = Changes.Direct("count");
        _changes = [_first, _join, _last];
    }

    void Because() => _result = ChangesetWriteOrder.SegmentsFor(_changes);

    [Fact] void should_split_into_two_segments() => _result!.Count.ShouldEqual(2);
    [Fact] void should_keep_the_first_direct_change_alone() => _result![0].ShouldContainOnly(_first);
    [Fact] void should_write_the_join_before_the_last_direct_change()
    {
        _result![1][0].ShouldEqual(_join);
        _result[1][1].ShouldEqual(_last);
    }
}
