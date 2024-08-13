// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_it_is_already_disposed : given.a_unit_of_work
{
    Exception _exception;

    void Establish()
    {
        _unitOfWork.Dispose();
    }

    async Task Because() => _exception = await Catch.Exception(_unitOfWork.Commit);

    [Fact] void should_throw_already_rolled_back() => _exception.ShouldBeOfExactType<UnitOfWorkIsAlreadyRolledBack>();
}
