// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_reads_are_protected : given.a_unit_of_work_manager
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_refuse_replacement_without_changing_the_ambient_unit(bool begin)
    {
        var unit = (UnitOfWork)_manager.Begin(CorrelationId.New());
        using var frame = unit.EnforceReadProtection();
        var owner = unit.ClaimDecisionReadCommitOwnership();
        var foreign = new UnitOfWork(CorrelationId.New(), _ => { }, _eventStore);
        var error = Catch.Exception(() =>
        {
            if (begin) _manager.Begin(CorrelationId.New());
            else _manager.SetCurrent(foreign);
        });
        error.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
        _manager.Current.ShouldBeSame(unit);
        unit.RollbackAsOwner(owner);
    }
}
