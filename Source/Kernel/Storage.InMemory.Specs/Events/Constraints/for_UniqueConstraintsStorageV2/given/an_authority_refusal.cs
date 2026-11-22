// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2.given;

public class an_authority_refusal : a_seeded_v2_store
{
    protected Result<bool, UniqueConstraintWriteFailure>[] _results;
    protected UniqueConstraintWriteAuthority _proof;
    protected UniqueConstraintWriteAuthority _current;
    protected UniqueConstraintValue[] _retained;
    protected UniqueConstraintValue[] _retainedOtherScope;
    protected EventSequenceNumber _sequence;
    protected bool _subjectRemoved;

    async Task Establish()
    {
        _proof = _authority;
        _current = _authority;
        (await _storage.Save(_source, _definition, 11, _otherValue, _otherScope, _authority, _subject)).IsSuccess.ShouldBeTrue();
    }

    protected async Task Refuse()
    {
        _results = await Task.WhenAll(Mutations(_proof));
        _retained = (await _storage.GetFor(_source, _definition, _scope)).ToArray();
        _retainedOtherScope = (await _storage.GetFor(_source, _definition, _otherScope)).ToArray();
        _sequence = (await _storage.IsAllowed(_source, _definition, _value, _scope)).SequenceNumber;
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_current);
        (await _storage.RemoveForSubject(_definition, _subject, _current)).IsSuccess.ShouldBeTrue();
        _subjectRemoved = !(await _storage.GetFor(_source, _definition, _scope)).Any();
    }

    protected async Task Transition(UniqueConstraintWritePhase phase)
    {
        var result = await _storage.TryTransition(_definition.Name, _current, phase);
        result.IsSuccess.ShouldBeTrue();
        _current = result.AsT0;
    }

    protected bool AllRefused => _results.All(_ => !_.IsSuccess && _.AsT1 == UniqueConstraintWriteFailure.AuthorityMismatch);
    protected bool ClaimsUnchanged => _retained.SequenceEqual([_value]) && _retainedOtherScope.SequenceEqual([_otherValue]) && _sequence == (EventSequenceNumber)10 && _subjectRemoved;
}
