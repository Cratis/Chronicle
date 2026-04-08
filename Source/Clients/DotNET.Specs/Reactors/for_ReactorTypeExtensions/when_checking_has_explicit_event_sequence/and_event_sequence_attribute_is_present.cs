// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_checking_has_explicit_event_sequence;

public class and_event_sequence_attribute_is_present : Specification
{
    [EventSequence("my-sequence")]
    [Reactor]
    class ReactorWithEventSequenceAttribute : IReactor;

    bool _result;

    void Because() => _result = typeof(ReactorWithEventSequenceAttribute).HasExplicitEventSequence();

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
