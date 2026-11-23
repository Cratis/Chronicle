// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public abstract partial class UniqueConstraintsStorageV2Contract
{
    [Fact]
    public async Task should_remove_only_the_selected_owner_and_scope()
    {
        await SeedRemoval();
        (await _storage.Remove(_owner, _definition, _scope, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldSeeClaims([], [_value], [_otherValue]);
    }

    [Fact]
    public async Task should_remove_only_the_owned_per_value_claim()
    {
        await Seed(PerValue);
        (await _storage.Save(_owner, PerValue, 11, _otherValue, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveValue(_otherOwner, PerValue, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(_owner, PerValue, _scope)).Count().ShouldEqual(2);
        (await _storage.RemoveValue(_owner, PerValue, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(_owner, PerValue, _scope)).ShouldContainOnly(_otherValue);
        await ShouldRetainAuthority();
    }

    [Fact]
    public async Task should_leave_per_event_source_claims_unchanged_on_remove_value()
    {
        await Seed();
        (await _storage.RemoveValue(_owner, _definition, _value, _scope, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_clear_only_the_selected_scope_and_mode()
    {
        await SeedRemoval();
        (await _storage.ClearValues(_definition, _scope, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldSeeClaims([], [_value], []);
    }

    [Fact]
    public async Task should_clear_all_scopes_but_not_the_other_mode_or_authority()
    {
        await SeedRemoval();
        (await _storage.RemoveAllFor(_definition, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldSeeClaims([], [], []);
    }

    [Fact]
    public async Task should_remove_subject_claims_across_scopes_without_crossing_mode()
    {
        await SeedRemoval();
        (await _storage.RemoveForSubject(_definition, _subject, _authority)).IsSuccess.ShouldBeTrue();
        await ShouldSeeClaims([], [], [_otherValue]);
    }

    [Fact]
    public async Task should_keep_long_subjects_lossless_and_distinct()
    {
        await Initialize();
        var prefix = new string('界', 2000);
        Subject subject = $"{prefix}a";
        Subject otherSubject = $"{prefix}b";
        (await _storage.Save(_owner, _definition, 10, _value, _scope, _authority, subject)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_otherOwner, _definition, 11, _otherValue, _scope, _authority, otherSubject)).IsSuccess.ShouldBeTrue();
        (await _storage.RemoveForSubject(_definition, subject, _authority)).IsSuccess.ShouldBeTrue();
        (await _storage.GetFor(_owner, _definition, _scope)).ShouldBeEmpty();
        (await _storage.GetFor(_otherOwner, _definition, _scope)).ShouldContainOnly(_otherValue);
    }

    [Fact]
    public async Task should_compare_subjects_exactly_including_trailing_spaces()
    {
        await Seed();
        (await _storage.RemoveForSubject(_definition, "subject ", _authority)).IsSuccess.ShouldBeTrue();
        await ShouldRetainClaim();
        (await _storage.RemoveForSubject(_definition, "SUBJECT", _authority)).IsSuccess.ShouldBeTrue();
        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_fence_no_op_mutations_without_creating_authority()
    {
        for (var operation = 1; operation < 6; operation++)
        {
            ShouldRefuse(await Mutation(operation, _authority), UniqueConstraintWriteFailure.MissingAuthority);
        }

        (await _storage.GetAuthority(_definition.Name)).ShouldEqual(Option<UniqueConstraintWriteAuthority>.None());
    }

    [Fact]
    public async Task should_accept_no_op_mutations_with_current_authority()
    {
        await Initialize();
        for (var operation = 1; operation < 6; operation++)
        {
            (await Mutation(operation, _authority)).IsSuccess.ShouldBeTrue();
        }

        await ShouldRetainAuthority();
    }

    async Task SeedRemoval()
    {
        await Seed();
        (await _storage.Save(_owner, _definition, 10, _value, _otherScope, _authority, _subject)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_otherOwner, _definition, 11, _otherValue, _scope, _authority, "other-subject")).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_owner, PerValue, 10, _value, _scope, _authority, _subject)).IsSuccess.ShouldBeTrue();
    }

    async Task ShouldSeeClaims(UniqueConstraintValue[] ownerInScope, UniqueConstraintValue[] ownerInOtherScope, UniqueConstraintValue[] otherOwnerInScope)
    {
        (await _storage.GetFor(_owner, _definition, _scope)).SequenceEqual(ownerInScope).ShouldBeTrue();
        (await _storage.GetFor(_owner, _definition, _otherScope)).SequenceEqual(ownerInOtherScope).ShouldBeTrue();
        (await _storage.GetFor(_otherOwner, _definition, _scope)).SequenceEqual(otherOwnerInScope).ShouldBeTrue();
        await ShouldRetainClaim(PerValue);
        await ShouldRetainAuthority();
    }

    async Task ShouldRetainAuthority() => (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_authority);
}
