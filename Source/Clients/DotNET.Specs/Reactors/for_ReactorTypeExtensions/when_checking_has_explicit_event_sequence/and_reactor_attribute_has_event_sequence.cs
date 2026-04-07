// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_checking_has_explicit_event_sequence;

public class and_reactor_attribute_has_event_sequence : Specification
{
    [Reactor(eventSequence: "my-sequence")]
    class ReactorWithExplicitEventSequenceInAttribute : IReactor;

    bool _result;

    void Because() => _result = typeof(ReactorWithExplicitEventSequenceInAttribute).HasExplicitEventSequence();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
