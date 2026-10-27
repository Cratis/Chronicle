// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Jobs.for_BoundedEventCursor;

public class when_disposing : given.a_cursor_over_batches
{
    protected override int Bound => 2;

    protected override IReadOnlyList<AppendedEvent[]> Batches => [];

    void Because() => _cursor.Dispose();

    [Fact] void should_dispose_the_inner_cursor() => _inner.Received(1).Dispose();
}
