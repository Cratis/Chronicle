// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetWriteOrder;

public class when_the_join_is_a_child_join : Specification
{
    Change[] _changes;
    IReadOnlyList<IReadOnlyList<Change>>? _result;

    void Establish()
    {
        _changes = [Changes.Direct("count"), new Joined(new ExpandoObject(), "key", new PropertyPath("joinId"), new ArrayIndexers([new ArrayIndexer(new PropertyPath("children"), new PropertyPath("id"), "c")]), [Changes.Direct("count")]) { HasKeyedFrom = true }];
    }

    void Because() => _result = ChangesetWriteOrder.SegmentsFor(_changes);

    [Fact] void should_use_the_single_pass() => _result.ShouldBeNull();
}
