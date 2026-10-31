// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_using_an_effect_receipt;

public class without_capabilities : given.a_unit_of_work
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => _unitOfWork.UseEffectReceipt(new("occurrence", "send", new("occurrence", 1)), null));

    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<EffectReceiptsNotSupported>();
    [Fact] void should_not_complete_the_unit() => _unitOfWork.IsCompleted.ShouldBeFalse();
}
