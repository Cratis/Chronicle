// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Grains.Specs.EventSequences.for_EventHashCalculator;

public class when_calculating_hash_with_empty_content : Specification
{
    EventHashCalculator _calculator;
    EventTypeId _eventTypeId;
    EventSourceId _eventSourceId;
    ExpandoObject _content;
    string _hash;

    void Establish()
    {
        _calculator = new EventHashCalculator();
        _eventTypeId = Guid.NewGuid().ToString();
        _eventSourceId = Guid.NewGuid().ToString();
        _content = new ExpandoObject();
    }

    void Because() => _hash = _calculator.Calculate(_eventTypeId, _eventSourceId, _content);

    [Fact] void should_produce_a_hash() => _hash.ShouldNotBeNull();
    [Fact] void should_produce_non_empty_hash() => _hash.ShouldNotBeEmpty();
}
