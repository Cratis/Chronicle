// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWorkManager;

public class when_getting_current_and_no_unit_of_work_has_been_started : given.a_unit_of_work_manager
{
    IUnitOfWork _result;
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _result = _manager.Current);

    [Fact] void should_throw_no_unit_of_work_has_been_started() => _exception.ShouldBeOfExactType<NoUnitOfWorkHasBeenStarted>();
}
