// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.EventSequences;

namespace Cratis.Chronicle.Grains.Specs.EventSequences.for_EventHashCalculator;

public class when_calculating_hash_with_same_content_but_different_event_type : Specification
{
    EventHashCalculator calculator;
    EventTypeId first_event_type_id;
    EventTypeId second_event_type_id;
    EventSourceId event_source_id;
    ExpandoObject content;
    string first_hash;
    string second_hash;

    void Establish()
    {
        calculator = new EventHashCalculator();
        first_event_type_id = Guid.NewGuid();
        second_event_type_id = Guid.NewGuid();
        event_source_id = Guid.NewGuid();

        content = new ExpandoObject();
        var dict = (IDictionary<string, object>)content;
        dict["name"] = "John";
        dict["age"] = 42;
    }

    void Because()
    {
        first_hash = calculator.Calculate(first_event_type_id, event_source_id, content);
        second_hash = calculator.Calculate(second_event_type_id, event_source_id, content);
    }

    [Fact] void should_produce_different_hashes() => first_hash.ShouldNotEqual(second_hash);
}
