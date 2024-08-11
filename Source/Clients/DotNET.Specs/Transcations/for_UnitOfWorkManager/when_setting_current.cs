// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_setting_current : given.a_unit_of_work_manager
{
    IUnitOfWork _unitOfWork;
    IUnitOfWork _current;
    bool _hasCurrent;
    CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.CorrelationId.Returns(_correlationId);
    }

    void Because()
    {
        _manager.SetCurrent(_unitOfWork);
        _current = _manager.Current;
        _hasCurrent = _manager.HasCurrent;
    }

    [Fact] void should_return_the_result() => _current.ShouldEqual(_unitOfWork);
    [Fact] void should_have_current() => _hasCurrent.ShouldBeTrue();
    [Fact] void should_hold_unit_of_work_by_its_correlation_id() => _manager.TryGetFor(_correlationId, out var _).ShouldBeTrue();
}
