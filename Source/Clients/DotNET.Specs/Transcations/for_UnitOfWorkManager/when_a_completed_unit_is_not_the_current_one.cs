// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_a_completed_unit_is_not_the_current_one : given.a_unit_of_work_manager
{
    IUnitOfWork _first;
    IUnitOfWork _second;
    IUnitOfWork? _current;
    bool _hasCurrent;

    void Because()
    {
        _first = _manager.Begin(CorrelationId.New());
        _second = _manager.Begin(CorrelationId.New());
        _first.Dispose();
        _hasCurrent = _manager.HasCurrent;
        _current = _hasCurrent ? _manager.Current : null;
    }

    [Fact] void should_still_have_current() => _hasCurrent.ShouldBeTrue();
    [Fact] void should_keep_the_second_unit_as_current() => _current.ShouldEqual(_second);
    [Fact] void should_no_longer_hold_the_first_unit() => _manager.TryGetFor(_first.CorrelationId, out var _).ShouldBeFalse();
    [Fact] void should_still_hold_the_second_unit() => _manager.TryGetFor(_second.CorrelationId, out var _).ShouldBeTrue();
}
