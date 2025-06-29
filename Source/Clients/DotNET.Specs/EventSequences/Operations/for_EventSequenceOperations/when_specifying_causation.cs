// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations;

public class when_specifying_causation : given.event_sequence_operations_without_any_operations
{
    Causation causation;
    EventSequenceOperations result;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _operations = new(_eventSequence);
        causation = new Causation(DateTimeOffset.Now, "TestCausation", new Dictionary<string, string>()
        {
            { "TestKey", "TestValue" }
        });
    }

    void Because() => result = _operations.WithCausation(causation);

    [Fact] void should_return_self() => result.ShouldEqual(_operations);
}
