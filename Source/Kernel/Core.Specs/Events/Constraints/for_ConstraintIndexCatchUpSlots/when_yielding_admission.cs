// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexCatchUpSlots;

public class when_yielding_admission : Specification
{
    readonly object[] _owners = [new(), new(), new(), new(), new()];
    ConstraintIndexCatchUpSlots _slots;
    bool _admittedWhileRefusing;
    bool _admittedAfterYield;

    void Establish() => _slots = ConstraintIndexCatchUpSlots.For(Substitute.For<IStorage>());

    void Because()
    {
        foreach (var owner in _owners.Take(4)) _slots.TryEnter(owner).ShouldBeTrue();
        _slots.Yield(_owners[0], refusing: true);
        _admittedWhileRefusing = _slots.TryEnter(_owners[4]);
        _slots.Yield(_owners[1], refusing: false);
        _admittedAfterYield = _slots.TryEnter(_owners[4]);
    }

    void Destroy()
    {
        foreach (var owner in _owners) _slots.Complete(owner);
    }

    [Fact] void should_keep_a_refusing_sequences_memory_admission() => _admittedWhileRefusing.ShouldBeFalse();
    [Fact] void should_admit_a_waiting_sequence_after_yielding() => _admittedAfterYield.ShouldBeTrue();
}
