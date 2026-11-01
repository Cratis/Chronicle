// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexCatchUpSlots;

public class when_two_slots_are_configured : Specification
{
    readonly object[] _owners = [new(), new(), new()];
    ConstraintIndexCatchUpSlots _slots;
    bool _thirdAdmitted;
    bool _admittedAfterCompletion;

    void Establish() => _slots = ConstraintIndexCatchUpSlots.For(Substitute.For<IStorage>(), 2);

    void Because()
    {
        _slots.TryEnter(_owners[0]).ShouldBeTrue();
        _slots.TryEnter(_owners[1]).ShouldBeTrue();
        _thirdAdmitted = _slots.TryEnter(_owners[2]);
        _slots.Complete(_owners[0]);
        _admittedAfterCompletion = _slots.TryEnter(_owners[2]);
    }

    [Fact] void should_refuse_a_third_sequence() => _thirdAdmitted.ShouldBeFalse();
    [Fact] void should_admit_after_one_completes() => _admittedAfterCompletion.ShouldBeTrue();
}
