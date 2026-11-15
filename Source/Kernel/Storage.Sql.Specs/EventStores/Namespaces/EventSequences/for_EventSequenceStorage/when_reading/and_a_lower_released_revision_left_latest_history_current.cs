// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading;

public class and_a_lower_released_revision_left_latest_history_current : given.an_event_with_revision_history
{
    async Task Establish()
    {
        await _storage.Revise(EventSequenceNumber.First, new(_eventType.Id, new(2)), CorrelationId.New(), [], [], DateTimeOffset.UtcNow, Content("higher"), "hash-higher");
        await ReleasedRevise();
    }
    async Task Because() => await Read();

    [Fact] void should_preserve_the_known_original_for_the_highest_generation() => _read.OriginalContent.ShouldContain("original");
    [Fact] void should_keep_the_released_read_generation() => _read.Context.Hash.Value.ShouldEqual("hash-released");
    [Fact] void should_keep_the_known_history() => _read.Revisions.Count().ShouldEqual(1);
}
