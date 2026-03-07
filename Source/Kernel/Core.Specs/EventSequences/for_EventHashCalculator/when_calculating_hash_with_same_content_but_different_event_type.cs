// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator;

public class when_calculating_hash_with_same_content_but_different_event_type : Specification
{
    EventHashCalculator _calculator;
    EventTypeId _firstEventTypeId;
    EventTypeId _secondEventTypeId;
    EventSourceId _eventSourceId;
    ExpandoObject _content;
    string _firstHash;
    string _secondHash;

    void Establish()
    {
        _calculator = new EventHashCalculator();
        _firstEventTypeId = Guid.NewGuid().ToString();
        _secondEventTypeId = Guid.NewGuid().ToString();
        _eventSourceId = Guid.NewGuid().ToString();

        _content = new ExpandoObject();
        var dict = (IDictionary<string, object>)_content!;
        dict["name"] = "John";
        dict["age"] = 42;
    }

    void Because()
    {
        _firstHash = _calculator.Calculate(_firstEventTypeId, _eventSourceId, _content);
        _secondHash = _calculator.Calculate(_secondEventTypeId, _eventSourceId, _content);
    }

    [Fact] void should_produce_different_hashes() => _firstHash.ShouldNotEqual(_secondHash);
}
