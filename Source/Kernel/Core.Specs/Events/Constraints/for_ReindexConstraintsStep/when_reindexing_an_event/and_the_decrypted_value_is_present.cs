// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_reindexing_an_event;

public class and_the_decrypted_value_is_present : given.a_unique_constraint_to_reindex
{
    const string Value = "jane@example.com";
    EventSourceId _eventSourceId;
    ExpandoObject _content;
    UniqueConstraintValue _expectedHashedValue;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _content = ContentWith(Value);
        _expectedHashedValue = new List<UniqueConstraintPropertyAndValue> { new(Property, Value) }.GetValue();
    }

    async Task Because() => await ReindexConstraintsStep.ReindexEvent(_definition, EventFor(_eventSourceId), _content, _seen, _validator, _storage);

    [Fact] void should_clear_any_existing_index_entry() => _storage.Received(1).Remove(_eventSourceId, _definition.Name, Arg.Any<string>());
    [Fact] void should_save_the_hash_of_the_decrypted_value() => _storage.Received(1).Save(_eventSourceId, _definition.Name, Arg.Any<EventSequenceNumber>(), _expectedHashedValue, Arg.Any<string>());
}
