// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_unit_of_work_created_by_manager_completes : given.a_unit_of_work_manager
{
    CorrelationId _correlationId;
    IUnitOfWork _result;
    bool _hasCurrent;

    void Establish()
    {
        _correlationId = CorrelationId.New();
    }

    async Task Because()
    {
        _result = _manager.Begin(_correlationId);
        await _result.DisposeAsync();
        _hasCurrent = _manager.HasCurrent;
    }

    [Fact] void should_not_have_the_unit_of_work_based_on_its_id() => _manager.TryGetFor(_correlationId, out var _).ShouldBeFalse();
    [Fact] void should_not_have_current() => _manager.HasCurrent.ShouldBeFalse();
    [Fact] void should_not_have_a_current() => _hasCurrent.ShouldBeFalse();
}
