// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorAttribute;

public class when_declaring_constructors : Specification
{
    ReactorAttribute _plain;
    ReactorAttribute _withEpoch;
    ReactorAttribute _withSequenceAndEpoch;

    void Because()
    {
        _plain = new("reactor", "sequence");
        _withEpoch = new("reactor", effectEpoch: 2);
        _withSequenceAndEpoch = new("reactor", "sequence", 3);
    }

    [Fact] void should_keep_the_two_parameter_constructor() => typeof(ReactorAttribute).GetConstructor([typeof(string), typeof(string)]).ShouldNotBeNull();
    [Fact] void should_default_the_epoch_to_zero() => _plain.EffectEpoch.ShouldEqual(0u);
    [Fact] void should_keep_the_sequence_with_the_old_constructor() => _plain.EventSequenceId!.Value.ShouldEqual("sequence");
    [Fact] void should_set_the_epoch() => _withEpoch.EffectEpoch.ShouldEqual(2u);
    [Fact] void should_leave_the_sequence_unset_with_only_an_epoch() => _withEpoch.EventSequenceId.ShouldBeNull();
    [Fact] void should_set_both_sequence_and_epoch() => (_withSequenceAndEpoch.EventSequenceId!.Value, _withSequenceAndEpoch.EffectEpoch).ShouldEqual(("sequence", 3u));
}
