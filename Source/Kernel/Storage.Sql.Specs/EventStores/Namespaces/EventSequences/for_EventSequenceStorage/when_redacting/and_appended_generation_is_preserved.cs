// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_redacting;

public class and_appended_generation_is_preserved : given.a_storage_for_appended_generation
{
    EventEntry _stored;
    AppendedEvent _read;

    async Task Establish() => (await _storage.AppendMany([EventAt(0, 2)])).IsSuccess.ShouldBeTrue();

    async Task Because()
    {
        await _storage.Redact(0, "reason", CorrelationId.NotSet, [], [], DateTimeOffset.UtcNow);
        _stored = Stored(0);
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_keep_the_original_generation() => _stored.AppendedGeneration.ShouldEqual((uint?)2);
    [Fact] void should_still_read_the_redaction_type() => _read.Context.EventType.Id.ShouldEqual(GlobalEventTypes.Redaction);
    [Fact] void should_still_read_the_redaction_generation() => _read.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
}
