// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_it_is_already_committed : given.a_unit_of_work
{
    Exception _exception;

    async Task Establish()
    {
        await _unitOfWork.Commit();
    }

    async Task Because() => _exception = await Catch.Exception(_unitOfWork.Commit);

    [Fact] void should_throw_already_committed() => _exception.ShouldBeOfExactType<UnitOfWorkIsAlreadyCommitted>();
}



