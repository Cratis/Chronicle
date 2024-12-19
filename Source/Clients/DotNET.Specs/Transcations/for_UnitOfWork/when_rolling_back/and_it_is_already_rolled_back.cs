// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_Back;

public class and_it_is_already_rolled_back : given.a_unit_of_work
{
    Exception _exception;

    async Task Establish()
    {
        await _unitOfWork.Rollback();
    }

    async Task Because() => _exception = await Catch.Exception(_unitOfWork.Rollback);

    [Fact] void should_throw_any_exception() => _exception.ShouldBeOfExactType<UnitOfWorkIsAlreadyRolledBack>();
}
