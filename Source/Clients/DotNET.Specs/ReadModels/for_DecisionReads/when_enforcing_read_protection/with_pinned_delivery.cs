// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_pinned_delivery : given.a_protected_reader
{
    Exception _ambientError;
    Exception _explicitError;
    Exception _streamError;
    Exception _detachedError;

    void Establish() => _definition.GenerationDelivery = EventGenerationDelivery.Pinned;

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        _ambientError = await Catch.Exception(() => _reader.Get<Model>("source"));
        _explicitError = await Catch.Exception(() => _reader.GetFor<Model>(_unit, "source"));
        _streamError = await Catch.Exception(() => _reader.GetFor<Model>(_unit, "source", "stream", "id"));
        using var allowance = _unit.AllowReads(ReadAllowance.Advisory, "Detached diagnostic");
        _detachedError = await Catch.Exception(() => _reader.GetDetached<Model>("source"));
    }

    [Fact] void should_refuse_ambient_acquisition() => ((DecisionReadRefused)_ambientError).Reason.ShouldEqual(DecisionReadRefusalReason.GenerationDeliveryNotSupported);
    [Fact] void should_refuse_explicit_acquisition() => ((DecisionReadRefused)_explicitError).Reason.ShouldEqual(DecisionReadRefusalReason.GenerationDeliveryNotSupported);
    [Fact] void should_refuse_stream_acquisition() => ((DecisionReadRefused)_streamError).Reason.ShouldEqual(DecisionReadRefusalReason.GenerationDeliveryNotSupported);
    [Fact] void should_share_the_refusal_with_detached_admission() => ((DecisionReadRefused)_detachedError).Reason.ShouldEqual(DecisionReadRefusalReason.GenerationDeliveryNotSupported);
    [Fact] void should_not_fold_a_different_delivery() => _folds.ShouldEqual(0);
}
