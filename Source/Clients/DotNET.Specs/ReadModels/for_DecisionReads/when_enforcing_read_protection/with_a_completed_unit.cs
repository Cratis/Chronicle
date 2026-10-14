// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_a_completed_unit : given.a_protected_reader
{
    Exception _ambientError;
    Exception _explicitError;
    Exception _streamError;

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        var owner = _unit.ClaimDecisionReadCommitOwnership();
        await _unit.CommitAsOwner(owner);
        _manager.HasCurrent.Returns(false);
        _ambientError = await Catch.Exception(() => _reader.Get<Model>("source"));
        _explicitError = await Catch.Exception(() => _reader.GetFor<Model>(_unit, "source"));
        _streamError = await Catch.Exception(() => _reader.GetFor<Model>(_unit, "source", "stream", "id"));
    }

    [Fact] void should_preserve_ambient_read_protection_after_completion() => _ambientError.ShouldBeOfExactType<ReadAfterProtectedCommit>();
    [Fact] void should_refuse_explicit_unit_acquisition() => _explicitError.ShouldBeOfExactType<ReadAfterProtectedCommit>();
    [Fact] void should_refuse_stream_acquisition() => _streamError.ShouldBeOfExactType<ReadAfterProtectedCommit>();
    [Fact] void should_not_fold_after_completion() => _folds.ShouldEqual(0);
}
