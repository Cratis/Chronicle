// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep;

public class when_reindexing_events_for_two_subjects_with_the_same_value : given.a_unique_constraint_to_reindex
{
    const string Value = "shared@example.com";
    EventSourceId _firstSubject;
    EventSourceId _secondSubject;
    UniqueConstraintValue _expectedHashedValue;

    void Establish()
    {
        _firstSubject = EventSourceId.New();
        _secondSubject = EventSourceId.New();
        _expectedHashedValue = new List<UniqueConstraintPropertyAndValue> { new(Property, Value) }.GetValue();
    }

    async Task Because()
    {
        await ReindexConstraintsStep.ReindexEvent(_definition, EventFor(_firstSubject), ContentWith(Value), _seen, _validator, _storage);
        await ReindexConstraintsStep.ReindexEvent(_definition, EventFor(_secondSubject), ContentWith(Value), _seen, _validator, _storage);
    }

    [Fact] void should_index_the_shared_hash_for_the_first_subject() => _storage.Received(1).Save(_firstSubject, _definition.Name, Arg.Any<EventSequenceNumber>(), _expectedHashedValue, Arg.Any<string>());
    [Fact] void should_index_the_same_shared_hash_for_the_second_subject() => _storage.Received(1).Save(_secondSubject, _definition.Name, Arg.Any<EventSequenceNumber>(), _expectedHashedValue, Arg.Any<string>());
}
