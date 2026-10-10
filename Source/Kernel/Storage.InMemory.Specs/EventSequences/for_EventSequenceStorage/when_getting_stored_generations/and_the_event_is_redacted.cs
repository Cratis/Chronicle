// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_getting_stored_generations;

public class and_the_event_is_redacted : given.a_storage_with_stored_generations
{
    StoredEventGenerations _snapshot;

    async Task Establish()
    {
        await _storage.Redact(0, "reason", CorrelationId.NotSet, [], [], DateTimeOffset.UtcNow);
    }

    async Task Because()
    {
        _snapshot = (await _storage.GetStoredGenerations(0))!;
    }

    [Fact] void should_report_the_redaction_marker() => _snapshot.EventTypeId.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_not_expose_the_old_generations() => _snapshot.Content.ContainsKey(new EventTypeGeneration(2)).ShouldBeFalse();
}
