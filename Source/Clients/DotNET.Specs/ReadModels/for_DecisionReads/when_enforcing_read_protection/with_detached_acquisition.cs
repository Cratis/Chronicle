// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_detached_acquisition : given.a_protected_reader
{
    Exception _sourceError;
    Exception _streamError;
    Exception _contributedError;

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        _sourceError = await Catch.Exception(() => _reader.GetDetached<Model>("source"));
        _streamError = await Catch.Exception(() => _reader.GetDetached<Model>("source", "stream", "id"));
        using var allowance = _unit.AllowReads(ReadAllowance.Contributed, "Stream acquisition");
        _contributedError = await Catch.Exception(() => _reader.GetDetached<Model>("source"));
    }

    [Fact] void should_refuse_source_acquisition() => _sourceError.ShouldBeOfExactType<DetachedReadWhileReadsAreProtected>();
    [Fact] void should_refuse_stream_acquisition() => _streamError.ShouldBeOfExactType<DetachedReadWhileReadsAreProtected>();
    [Fact] void should_not_treat_contributed_as_advisory() => _contributedError.ShouldBeOfExactType<DetachedReadWhileReadsAreProtected>();
    [Fact] void should_not_start_a_fold() => _folds.ShouldEqual(0);
}
