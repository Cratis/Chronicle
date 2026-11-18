// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.InMemory.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_redacting_events;

public class and_there_is_no_event_at_the_sequence_number : given.a_storage_with_appended_events
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _storage.Redact(99, "reason", CorrelationId.New(), [], [], DateTimeOffset.UtcNow));

    [Fact] void should_throw_no_event_at_sequence_number() => _error.ShouldBeOfExactType<NoEventAtSequenceNumber>();
}
