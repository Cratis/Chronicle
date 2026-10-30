// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_redacting;

public class and_the_event_was_revised : given.a_storage_with_two_stored_generations
{
    EventEntry _stored;
    AppendedEvent _read;

    async Task Because()
    {
        await ReviseAt(0, 1, "revised", "revision-hash");
        await _storage.Redact(0, "reason", CorrelationId.NotSet, [], [], DateTimeOffset.UtcNow);
        _stored = Stored(0);
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_clear_the_revised_generation() => _stored.RevisedGeneration.ShouldBeNull();
    [Fact] void should_clear_the_revisions() => _stored.Revisions.ShouldBeNull();
    [Fact] void should_read_the_redaction_type() => _read.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_not_be_revised() => _read.IsRevised.ShouldBeFalse();
}
