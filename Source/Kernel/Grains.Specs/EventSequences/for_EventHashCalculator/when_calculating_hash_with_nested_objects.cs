// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.EventSequences;

namespace Cratis.Chronicle.Grains.Specs.EventSequences.for_EventHashCalculator;

public class when_calculating_hash_with_nested_objects : Specification
{
    EventHashCalculator calculator;
    EventTypeId event_type_id;
    EventSourceId event_source_id;
    ExpandoObject first_content;
    ExpandoObject second_content;
    string first_hash;
    string second_hash;

    void Establish()
    {
        calculator = new EventHashCalculator();
        event_type_id = Guid.NewGuid();
        event_source_id = Guid.NewGuid();

        first_content = new ExpandoObject();
        var firstDict = (IDictionary<string, object>)first_content;
        var firstAddress = new ExpandoObject();
        var firstAddressDict = (IDictionary<string, object>)firstAddress;
        firstAddressDict["city"] = "Oslo";
        firstAddressDict["street"] = "Main Street";
        firstDict["name"] = "John";
        firstDict["address"] = firstAddress;

        second_content = new ExpandoObject();
        var secondDict = (IDictionary<string, object>)second_content;
        var secondAddress = new ExpandoObject();
        var secondAddressDict = (IDictionary<string, object>)secondAddress;
        secondAddressDict["street"] = "Main Street";
        secondAddressDict["city"] = "Oslo";
        secondDict["address"] = secondAddress;
        secondDict["name"] = "John";
    }

    void Because()
    {
        first_hash = calculator.Calculate(event_type_id, event_source_id, first_content);
        second_hash = calculator.Calculate(event_type_id, event_source_id, second_content);
    }

    [Fact] void should_produce_identical_hashes_despite_different_property_order_in_nested_objects() => first_hash.ShouldEqual(second_hash);
}
