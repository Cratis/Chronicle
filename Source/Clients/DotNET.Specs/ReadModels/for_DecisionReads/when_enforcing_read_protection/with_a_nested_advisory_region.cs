// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_a_nested_advisory_region : given.a_protected_reader
{
    Exception _outsideError;
    bool _enforcing;

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        await _reader.GetFor<Model>(_unit, "source");
        using (var advisory = _unit.AllowReads(ReadAllowance.Advisory, "nested Off command"))
        {
            await _reader.GetDetached<Model>("source");
            _enforcing = _unit.IsEnforcingReadProtection;
        }
        _outsideError = await Catch.Exception(() => _reader.GetDetached<Model>("source"));
        _unit.AddEvent(EventSequenceId.Log, "source", new Created(), Causation.Unknown());
    }

    [Fact] void should_not_downgrade_the_unit() => _enforcing.ShouldBeTrue();
    [Fact] void should_restore_detached_read_refusal_after_the_region() => _outsideError.ShouldBeOfExactType<DetachedReadWhileReadsAreProtected>();
    [Fact] void should_allow_decide_then_stage_in_the_enforcing_unit() => _unit.GetEvents().Count().ShouldEqual(1);
    [Fact] void should_only_fold_the_two_allowed_acquisitions() => _folds.ShouldEqual(2);
}
