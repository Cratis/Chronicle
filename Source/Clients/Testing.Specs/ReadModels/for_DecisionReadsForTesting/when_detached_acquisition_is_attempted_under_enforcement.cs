// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_detached_acquisition_is_attempted_under_enforcement : Specification
{
    Exception _sourceError;
    Exception _streamError;
    Exception _contributedError;
    DecisionRead<SimpleModule> _advisory;

    const string Key = "6f1c2d3e-4a5b-4c6d-8e7f-90a1b2c3d4e5";

    async Task Because()
    {
        var store = new EventStoreForTesting();
        var unit = store.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = unit.EnforceReadProtection();
        var reader = store.GetDecisionReads();
        _sourceError = await Catch.Exception(() => reader.GetDetached<SimpleModule>(Key));
        _streamError = await Catch.Exception(() => reader.GetDetached<SimpleModule>(Key, "stream", "id"));
        using (var allowance = unit.AllowReads(ReadAllowance.Contributed, "Stream transport"))
        {
            _contributedError = await Catch.Exception(() => reader.GetDetached<SimpleModule>(Key));
        }
        using (var allowance = unit.AllowReads(ReadAllowance.Advisory, "Reference context"))
        {
            _advisory = await reader.GetDetached<SimpleModule>(Key);
        }
    }

    [Fact] void should_refuse_source_acquisition() => _sourceError.ShouldBeOfExactType<DetachedReadWhileReadsAreProtected>();
    [Fact] void should_refuse_stream_acquisition() => _streamError.ShouldBeOfExactType<DetachedReadWhileReadsAreProtected>();
    [Fact] void should_refuse_detached_contributed_acquisition() => _contributedError.ShouldBeOfExactType<DetachedReadWhileReadsAreProtected>();
    [Fact] void should_allow_explicit_advisory_acquisition() => _advisory.Instance.ShouldBeNull();
}
