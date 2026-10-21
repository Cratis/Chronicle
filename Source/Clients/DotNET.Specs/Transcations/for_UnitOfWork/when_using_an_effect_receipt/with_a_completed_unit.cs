// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_using_an_effect_receipt;

public class with_a_completed_unit : given.a_receipted_unit
{
    Exception _error;

    Task Establish() => _unitOfWork.Commit();

    void Because() => _error = Catch.Exception(() => _unitOfWork.UseEffectReceipt(_key));

    [Fact] void should_refuse_enrollment_after_completion() => _error.ShouldBeOfExactType<UnitOfWorkIsAlreadyCommitted>();
}
