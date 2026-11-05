// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_redacting;

public class by_event_source_with_recorded_history : given.an_event_with_revision_history
{
    async Task Establish() => await Revise();
    async Task Because()
    {
        await Read();
        await _storage.Redact(new EventSourceId(_entry.EventSourceId), "reason", [], CorrelationId.New(), [], [], DateTimeOffset.UtcNow);
        await Read();
    }

    [Fact] void should_clear_the_original_envelope() => _entry.OriginalContent.ShouldBeNull();
    [Fact] void should_clear_the_revision_array() => _entry.Revisions.ShouldBeNull();
    [Fact] void should_not_expose_history() => _read.Revisions.Count().ShouldEqual(0);
}
