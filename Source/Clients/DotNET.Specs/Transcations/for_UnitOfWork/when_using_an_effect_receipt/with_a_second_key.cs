// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_using_an_effect_receipt;

public class with_a_second_key : given.a_receipted_unit
{
    Exception _exception;

    void Establish() => _unitOfWork.UseEffectReceipt(_key);
    void Because() => _exception = Catch.Exception(() => _unitOfWork.UseEffectReceipt(_key with { Name = "another" }));

    [Fact] void should_refuse_a_second_effect() => _exception.ShouldBeOfExactType<UnitOfWorkEffectReceiptAlreadySet>();
}
