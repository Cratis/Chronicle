// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Grains.Specs.EventSequences.for_EventHashCalculator;

public class when_calculating_hash_with_nested_objects : Specification
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
        var firstAddress = new ExpandoObject();
        var firstAddressDict = (IDictionary<string, object>)firstAddress!;
        firstAddressDict["city"] = "Oslo";
        firstAddressDict["street"] = "Main Street";
        firstDict["name"] = "John";
        firstDict["address"] = firstAddress;

        _secondContent = new ExpandoObject();
        var secondDict = (IDictionary<string, object>)_secondContent!;
        var secondAddress = new ExpandoObject();
        var secondAddressDict = (IDictionary<string, object>)secondAddress!;
        secondAddressDict["street"] = "Main Street";
        secondAddressDict["city"] = "Oslo";
        secondDict["address"] = secondAddress;
        secondDict["name"] = "John";
    }

    void Because()
    {
        _firstHash = _calculator.Calculate(_eventTypeId, _eventSourceId, _firstContent);
        _secondHash = _calculator.Calculate(_eventTypeId, _eventSourceId, _secondContent);
    }

    [Fact] void should_produce_identical_hashes_despite_different_property_order_in_nested_objects() => _firstHash.ShouldEqual(_secondHash);
}
