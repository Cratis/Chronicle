// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_comitting;

public class and_then_resetting_and_then_committing : given.a_unit_of_work
{
    Exception _exception;

    async Task Establish()
    {
        await _unitOfWork.Commit();
        await _unitOfWork.Reset();
    }

    async Task Because() => _exception = await Catch.Exception(_unitOfWork.Commit);

    [Fact] void should_not_throw_already_committed() => _exception.ShouldBeNull();
}
