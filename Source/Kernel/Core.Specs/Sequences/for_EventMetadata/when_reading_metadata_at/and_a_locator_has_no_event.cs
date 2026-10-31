// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_EventMetadata.when_reading_metadata_at;

public class and_a_locator_has_no_event : given.a_metadata_query
{
    async Task Because() => await Read(42, 1000, 42);

    [Fact] void should_omit_the_missing_locator() => _result.Select(_ => _.SequenceNumber.Value).ShouldContainOnly(42UL);
    [Fact] void should_remove_duplicate_locators_before_storage() => _events.Received(1).GetMetadataAt(Arg.Is<IEnumerable<EventSequenceNumber>>(_ => _.Select(number => number.Value).SequenceEqual(new ulong[] { 42, 1000 })), Arg.Any<CancellationToken>());
}
