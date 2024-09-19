// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_setting_current_and_it_completes : given.a_unit_of_work_manager
{
    IUnitOfWork _unitOfWork;
    bool _hasCurrent;
    CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _unitOfWork = new UnitOfWork(_correlationId, _ => { }, _eventStore);
    }

    async Task Because()
    {
        _manager.SetCurrent(_unitOfWork);
        await _unitOfWork.DisposeAsync();
        _hasCurrent = _manager.HasCurrent;
    }

    [Fact] void should_not_have_current() => _hasCurrent.ShouldBeFalse();
    [Fact] void should_not_have_unit_of_work_based_on_its_id() => _manager.TryGetFor(_correlationId, out var _).ShouldBeFalse();
}
