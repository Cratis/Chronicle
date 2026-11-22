// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorageV2;

public class when_replacement_conflicts : given.a_seeded_v2_store
{
    Result<bool, UniqueConstraintWriteFailure> _result;
    async Task Establish() => (await _storage.Save(_otherSource, _definition, 11, _otherValue, _scope, _authority)).IsSuccess.ShouldBeTrue();
    async Task Because() => _result = await _storage.Save(_source, _definition, 20, _otherValue, _scope, _authority);
    [Fact] void should_refuse_the_conflict() => _result.AsT1.ShouldEqual(UniqueConstraintWriteFailure.ConflictingClaim);
    [Fact] async Task should_retain_the_old_value() => (await _storage.GetFor(_source, _definition, _scope)).ShouldEqual([_value]);
    [Fact] async Task should_leave_the_other_owner_unchanged() => (await _storage.IsAllowed(_otherSource, _definition, _otherValue, _scope)).SequenceNumber.Value.ShouldEqual(11UL);
}
