// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_using_an_effect_receipt;

public class with_unknown_capabilities : given.a_receipted_unit
{
    Exception _exception;

    void Establish() => ((IKernelCapabilities)_connection).CapabilitiesAreKnown.Returns(false);
    void Because() => _exception = Catch.Exception(() => _unitOfWork.UseEffectReceipt(_key));

    [Fact] void should_refuse_even_if_the_unknown_set_contains_the_capability() => _exception.ShouldBeOfExactType<EffectReceiptsNotSupported>();
}
