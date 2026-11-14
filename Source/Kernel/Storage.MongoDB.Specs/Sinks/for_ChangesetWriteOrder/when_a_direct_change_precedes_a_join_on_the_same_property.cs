// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetWriteOrder;

public class when_a_direct_change_precedes_a_join_on_the_same_property : Specification
{
    Change[] _changes;
    PropertiesChanged<ExpandoObject> _direct;
    Joined _join;
    IReadOnlyList<IReadOnlyList<Change>>? _result;

    void Establish()
    {
        _direct = Changes.Direct("count");
        _join = Changes.Join(true, "count");
        _changes = [_direct, _join];
    }

    void Because() => _result = ChangesetWriteOrder.SegmentsFor(_changes);

    [Fact] void should_split_into_two_segments() => _result!.Count.ShouldEqual(2);
    [Fact] void should_write_the_direct_change_first() => _result![0].ShouldContainOnly(_direct);
    [Fact] void should_write_the_join_second() => _result![1].ShouldContainOnly(_join);
}
