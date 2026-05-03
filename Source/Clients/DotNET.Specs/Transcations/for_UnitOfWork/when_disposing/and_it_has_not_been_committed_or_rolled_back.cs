// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_disposing;

public class and_it_has_not_been_committed_or_rolled_back : given.a_unit_of_work
{
    int _onCompletedCallCount;

    void Establish()
    {
        _onCompletedCallCount = 0;
        _unitOfWork.OnCompleted(_ => _onCompletedCallCount++);
    }

    void Because() => _unitOfWork.Dispose();

    [Fact] void should_call_on_completed_exactly_once() => _onCompletedCallCount.ShouldEqual(1);
    [Fact] void should_be_rolled_back() => _unitOfWork.IsCompleted.ShouldBeTrue();
}
