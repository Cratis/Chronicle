// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public abstract partial class UniqueConstraintsStorageV2Contract
{
    [Fact]
    public async Task should_reopen_authority_and_both_retention_modes()
    {
        await Seed();
        (await _storage.Save(_owner, PerValue, 11, _otherValue, _otherScope, _authority, _subject)).IsSuccess.ShouldBeTrue();
        _storage = await ReopenStorage();
        await ShouldRetainAuthority();
        await ShouldRetainClaim();
        (await _storage.GetFor(_owner, PerValue, _otherScope)).ShouldContainOnly(_otherValue);
        (await _storage.IsAllowed(_otherOwner, PerValue, _otherValue, _otherScope)).IsAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task should_reopen_a_transition_without_reviving_the_old_proof()
    {
        await Seed();
        var next = await _storage.TryTransition(_definition.Name, _authority, Run(1));
        next.IsSuccess.ShouldBeTrue();
        _storage = await ReopenStorage();
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)next.AsT0);
        for (var operation = 0; operation < 6; operation++)
        {
            ShouldRefuse(await Mutation(operation, _authority), UniqueConstraintWriteFailure.AuthorityMismatch);
        }

        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_reopen_subject_metadata_without_narrowing_it()
    {
        await Seed();
        (await _storage.Save(_owner, _definition, 99, _value, _scope, _authority, "updated-subject")).IsSuccess.ShouldBeTrue();
        _storage = await ReopenStorage();
        (await _storage.RemoveForSubject(_definition, _subject, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldRetainClaim();
        (await _storage.RemoveForSubject(_definition, "updated-subject", _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(_owner, _definition, _scope)).ShouldBeEmpty();
    }
}
