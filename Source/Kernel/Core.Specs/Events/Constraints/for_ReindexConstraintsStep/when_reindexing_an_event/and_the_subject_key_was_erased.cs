// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_reindexing_an_event;

public class and_the_subject_key_was_erased : given.a_unique_constraint_to_reindex
{
    EventSourceId _eventSourceId;
    ExpandoObject _content;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();

        // Right-to-erasure removed the encryption key, so releasing the PII yields an empty value.
        _content = ContentWith(string.Empty);
    }

    async Task Because() => await ReindexConstraintsStep.ReindexEvent(_definition, EventFor(_eventSourceId), _content, _seen, _validator, _storage);

    [Fact] void should_clear_any_existing_index_entry() => _storage.Received(1).Remove(_eventSourceId, _definition.Name, Arg.Any<string>());
    [Fact] void should_not_index_a_hash_of_the_empty_value() => _storage.DidNotReceive().Save(Arg.Any<EventSourceId>(), Arg.Any<ConstraintName>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>());
}
