// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Specs.EventSequences.for_EventHashCalculator;

public class when_calculating_hash_with_different_property_order : Specification
{
    EventHashCalculator _calculator;
    EventTypeId _eventTypeId;
    EventSourceId _eventSourceId;
    ExpandoObject _firstContent;
    ExpandoObject _secondContent;
    string _firstHash;
    string _secondHash;

    void Establish()
    {
        _calculator = new EventHashCalculator();
        _eventTypeId = Guid.NewGuid().ToString();
        _eventSourceId = Guid.NewGuid().ToString();

        _firstContent = new ExpandoObject();
        var firstDict = (IDictionary<string, object>)_firstContent!;
        firstDict["name"] = "John";
        firstDict["age"] = 42;
        firstDict["email"] = "john@example.com";

        _secondContent = new ExpandoObject();
        var secondDict = (IDictionary<string, object>)_secondContent!;
        secondDict["email"] = "john@example.com";
        secondDict["name"] = "John";
        secondDict["age"] = 42;
    }

    void Because()
    {
        _firstHash = _calculator.Calculate(_eventTypeId, _eventSourceId, _firstContent);
        _secondHash = _calculator.Calculate(_eventTypeId, _eventSourceId, _secondContent);
    }

    [Fact] void should_produce_identical_hashes() => _firstHash.ShouldEqual(_secondHash);
}
