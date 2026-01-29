// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.EventSequences;

namespace Cratis.Chronicle.Grains.Specs.EventSequences.for_EventHashCalculator;

public class when_calculating_hash_with_empty_content : Specification
{
    EventHashCalculator calculator;
    EventTypeId event_type_id;
    EventSourceId event_source_id;
    ExpandoObject content;
    string hash;

    void Establish()
    {
        calculator = new EventHashCalculator();
        event_type_id = Guid.NewGuid();
        event_source_id = Guid.NewGuid();
        content = new ExpandoObject();
    }

    void Because() => hash = calculator.Calculate(event_type_id, event_source_id, content);

    [Fact] void should_produce_a_hash() => hash.ShouldNotBeNull();
    [Fact] void should_produce_non_empty_hash() => hash.ShouldNotBeEmpty();
}
