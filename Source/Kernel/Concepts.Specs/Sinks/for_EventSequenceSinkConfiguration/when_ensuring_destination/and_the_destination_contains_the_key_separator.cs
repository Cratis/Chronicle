// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Sinks.for_EventSequenceSinkConfiguration.when_ensuring_destination;

public class and_the_destination_contains_the_key_separator : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => new EventSequenceSinkConfiguration(new EventType("Changed", EventTypeGeneration.First), "observers#store#tenant").EnsureDestinationAllowed());

    [Fact] void should_refuse_the_destination() => _error.ShouldBeOfExactType<EventSequenceDestinationContainsKeySeparator>();
}
