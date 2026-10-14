// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_get_for_receives_a_foreign_unit : Specification
{
    Exception _sourceError;
    Exception _streamError;

    async Task Because()
    {
        var store = new EventStoreForTesting();
        var unit = store.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = unit.EnforceReadProtection();
        var foreign = new UnitOfWork(CorrelationId.New(), _ => { }, store);
        var reader = store.GetDecisionReads();
        _sourceError = await Catch.Exception(() => reader.GetFor<SimpleModule>(foreign, "source"));
        _streamError = await Catch.Exception(() => reader.GetFor<SimpleModule>(foreign, "source", "stream", "id"));
    }

    [Fact] void should_bind_source_acquisition_to_the_invocation() => _sourceError.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
    [Fact] void should_bind_stream_acquisition_to_the_invocation() => _streamError.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
}
