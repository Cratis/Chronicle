// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetWriteOrder;

public class when_no_join_has_a_keyed_from : Specification
{
    Change[] _changes;
    IReadOnlyList<IReadOnlyList<Change>>? _result;

    void Establish()
    {
        _changes = [Changes.Direct("count"), Changes.Join(false, "count")];
    }

    void Because() => _result = ChangesetWriteOrder.SegmentsFor(_changes);

    [Fact] void should_use_the_single_pass() => _result.ShouldBeNull();
}
