// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.EventSequences;

namespace Cratis.Chronicle.Grains.Specs.EventSequences.for_EventHashCalculator;

public class when_calculating_hash_with_different_content : Specification
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
        firstDict["name"] = "John";
        firstDict["age"] = 42;

        second_content = new ExpandoObject();
        var secondDict = (IDictionary<string, object>)second_content;
        secondDict["name"] = "Jane";
        secondDict["age"] = 42;
    }

    void Because()
    {
        first_hash = calculator.Calculate(event_type_id, event_source_id, first_content);
        second_hash = calculator.Calculate(event_type_id, event_source_id, second_content);
    }

    [Fact] void should_produce_different_hashes() => first_hash.ShouldNotEqual(second_hash);
}
