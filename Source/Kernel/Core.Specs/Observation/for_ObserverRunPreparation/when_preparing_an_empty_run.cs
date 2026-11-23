// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_ObserverRunPreparation;

public class when_preparing_an_empty_run : Specification
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ObserverRunPreparation.Prepare([], new(0UL, true, new HashSet<EventTypeId>(), null)));

    [Fact] void should_preserve_the_nonempty_precondition_of_the_private_handler() => _exception.ShouldBeOfExactType<InvalidOperationException>();
}
