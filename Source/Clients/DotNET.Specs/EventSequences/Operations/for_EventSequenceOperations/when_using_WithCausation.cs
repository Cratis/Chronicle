// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;

namespace Cratis.Chronicle.EventSequences.Operations.for_EventSequenceOperations;

public class when_using_WithCausation : given.event_sequence_operations_without_any_operations
{
    Causation causation;
    EventSequenceOperations result;

    void Establish()
    {
        eventSequence = Substitute.For<IEventSequence>();
        operations = new(eventSequence);
        causation = new Causation(DateTimeOffset.Now, "TestCausation", new Dictionary<string, string>()
        {
            { "TestKey", "TestValue" }
        });
    }

    void Because() => result = operations.WithCausation(causation);

    [Fact] void should_return_self() => result.ShouldEqual(operations);
}
