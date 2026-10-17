// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexCatchUpSlots;

public class when_admitting_sequences : Specification
{
    readonly object[] _owners = [new(), new(), new(), new(), new()];
    ConstraintIndexCatchUpSlots _slots;
    bool _fifthAdmitted;
    bool _refusingOwnerRetained;
    bool _admittedAfterCompletion;

    void Establish() => _slots = ConstraintIndexCatchUpSlots.For(Substitute.For<IStorage>());

    void Because()
    {
        foreach (var owner in _owners.Take(4)) _slots.TryEnter(owner).ShouldBeTrue();
        _slots.LeaveTurn(_owners[0], true);
        _refusingOwnerRetained = _slots.TryEnter(_owners[0]);
        foreach (var owner in _owners.Take(4)) _slots.LeaveTurn(owner, false);
        _fifthAdmitted = _slots.TryEnter(_owners[4]);
        _slots.Complete(_owners[0]);
        _admittedAfterCompletion = _slots.TryEnter(_owners[4]);
    }

    [Fact] void should_bound_retained_catch_up_memory_to_four_sequences() => _fifthAdmitted.ShouldBeFalse();
    [Fact] void should_keep_a_refusing_sequences_slot() => _refusingOwnerRetained.ShouldBeTrue();
    [Fact] void should_release_admission_after_completion() => _admittedAfterCompletion.ShouldBeTrue();
}
