// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetWriteOrder;

public class when_a_direct_path_is_an_ancestor_of_the_joined_path : Specification
{
    Change[] _changes;
    IReadOnlyList<IReadOnlyList<Change>>? _result;

    void Establish()
    {
        _changes = [Changes.Direct("address"), Changes.Join(true, "address.city")];
    }

    void Because() => _result = ChangesetWriteOrder.SegmentsFor(_changes);

    [Fact] void should_split_into_two_segments() => _result!.Count.ShouldEqual(2);
}
