// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Occurrences.for_EffectReceipts.when_committing;

public class against_an_old_kernel : given.a_store
{
    Exception? _error;

    void Establish() => _store.Capabilities.Old();

    void Because()
    {
        using var unit = new UnitOfWork(CorrelationId.New(), _ => { }, _store);
        _error = Catch.Exception(() => unit.UseEffectReceipt(_key));
    }

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<EffectReceiptsNotSupported>();
}
