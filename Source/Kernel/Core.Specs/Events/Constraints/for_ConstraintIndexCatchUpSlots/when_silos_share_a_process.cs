// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexCatchUpSlots;

public class when_silos_share_a_process : Specification
{
    bool _otherSiloAdmitted;
    ConstraintIndexCatchUpSlots _sameSilo;
    ConstraintIndexCatchUpSlots _first;

    void Because()
    {
        var storage = Substitute.For<IStorage>();
        _first = ConstraintIndexCatchUpSlots.For(storage);
        _sameSilo = ConstraintIndexCatchUpSlots.For(storage);
        for (var index = 0; index < 4; index++) _first.TryEnter(new object());
        _otherSiloAdmitted = ConstraintIndexCatchUpSlots.For(Substitute.For<IStorage>()).TryEnter(new object());
    }

    [Fact] void should_share_limits_within_the_silo() => _sameSilo.ShouldBeSame(_first);
    [Fact] void should_not_use_a_process_wide_limit() => _otherSiloAdmitted.ShouldBeTrue();
}
