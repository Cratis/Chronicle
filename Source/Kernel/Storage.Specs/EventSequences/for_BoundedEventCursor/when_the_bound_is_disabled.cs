// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.for_BoundedEventCursor;

public class when_the_bound_is_disabled : Specification
{
    BoundedEventCursor _cursor;
    List<ulong[]> _batches;

    void Establish() => _cursor = new(new MaterializedEventCursor(given.events.Numbered(1, 7)), 0);

    async Task Because() => _batches = await given.events.Walk(_cursor);

    [Fact] void should_leave_the_batch_as_it_is() => _batches.Single().Length.ShouldEqual(7);
}
