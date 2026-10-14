// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_another_receiving_unit : given.a_protected_reader
{
    Exception _sourceError;
    Exception _streamError;
    Exception _ambientError;

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        var other = new UnitOfWork(CorrelationId.New(), _ => { }, _store);
        _sourceError = await Catch.Exception(() => _reader.GetFor<Model>(other, "source"));
        _streamError = await Catch.Exception(() => _reader.GetFor<Model>(other, "source", "stream", "id"));
        _manager.Current.Returns(other);
        _ambientError = await Catch.Exception(() => _reader.Get<Model>("source"));
    }

    [Fact] void should_bind_source_reads_to_the_frame() => _sourceError.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
    [Fact] void should_bind_stream_reads_to_the_frame() => _streamError.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
    [Fact] void should_refuse_a_replacement_ambient_unit() => _ambientError.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
    [Fact] void should_refuse_before_folding() => _folds.ShouldEqual(0);
}
