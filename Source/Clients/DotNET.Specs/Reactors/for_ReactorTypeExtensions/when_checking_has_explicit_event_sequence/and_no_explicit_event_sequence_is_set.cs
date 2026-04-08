// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_checking_has_explicit_event_sequence;

public class and_no_explicit_event_sequence_is_set : Specification
{
    [Reactor]
    class ReactorWithoutExplicitEventSequence : IReactor;

    bool _result;

    void Because() => _result = typeof(ReactorWithoutExplicitEventSequence).HasExplicitEventSequence();

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
