// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_EventMetadata.when_reading_metadata_at;

public class and_exactly_the_maximum_locators_are_requested : given.a_metadata_query
{
    async Task Because() => await Read(Enumerable.Repeat(42UL, 500).ToArray());

    [Fact] void should_accept_the_limit() => _result.Length.ShouldEqual(1);
    [Fact] void should_deduplicate_before_reading_storage() => _events.Received(1).GetMetadataAt(Arg.Is<IEnumerable<EventSequenceNumber>>(_ => _.Count() == 1), Arg.Any<CancellationToken>());
}
