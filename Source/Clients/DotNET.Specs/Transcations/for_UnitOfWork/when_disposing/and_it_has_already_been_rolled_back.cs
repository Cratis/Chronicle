// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_disposing;

public class and_it_has_already_been_rolled_back : given.a_unit_of_work
{
    int _onCompletedCallCount;

    async Task Establish()
    {
        _onCompletedCallCount = 0;
        _unitOfWork.OnCompleted(_ => _onCompletedCallCount++);
        await _unitOfWork.Rollback();
    }

    void Because() => _unitOfWork.Dispose();

    [Fact] void should_not_call_on_completed_again() => _onCompletedCallCount.ShouldEqual(1);
}
