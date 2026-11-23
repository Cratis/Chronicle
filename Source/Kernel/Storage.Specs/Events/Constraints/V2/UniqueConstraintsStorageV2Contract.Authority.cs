// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public abstract partial class UniqueConstraintsStorageV2Contract
{
    [Fact]
    public async Task should_read_missing_authority_without_initializing_it()
    {
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual(Option<UniqueConstraintWriteAuthority>.None());
        ShouldRefuse(await _storage.TryTransition(_definition.Name, _authority, Run(1)), UniqueConstraintWriteFailure.MissingAuthority);
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual(Option<UniqueConstraintWriteAuthority>.None());
    }

    [Fact]
    public async Task should_initialize_once_and_retain_the_supplied_revision()
    {
        var proposed = _authority with { Revision = new(123) };
        var result = await _storage.InitializeAuthority(_definition.Name, proposed);
        result.IsSuccess.ShouldBeTrue();
        result.AsT0.ShouldEqual(proposed);
        ShouldRefuse(await _storage.InitializeAuthority(_definition.Name, proposed), UniqueConstraintWriteFailure.AuthorityMismatch);
        ShouldRefuse(await _storage.InitializeAuthority(_definition.Name, _authority), UniqueConstraintWriteFailure.AuthorityMismatch);
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)proposed);
    }

    [Fact]
    public async Task should_increment_revision_for_same_phase_transitions()
    {
        await Seed();
        var transitioned = await _storage.TryTransition(_definition.Name, _authority, UniqueConstraintWritePhase.Live);
        transitioned.IsSuccess.ShouldBeTrue();
        transitioned.AsT0.ShouldEqual(_authority with { Revision = new(1) });
        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_not_reuse_a_proof_when_returning_to_live()
    {
        await Initialize();
        var rebuilding = await _storage.TryTransition(_definition.Name, _authority, Run(1));
        rebuilding.IsSuccess.ShouldBeTrue();
        var live = await _storage.TryTransition(_definition.Name, rebuilding.AsT0, UniqueConstraintWritePhase.Live);
        live.IsSuccess.ShouldBeTrue();
        live.AsT0.ShouldEqual(_authority with { Revision = new(2) });
        ShouldRefuse(await _storage.TryTransition(_definition.Name, _authority, Run(2)), UniqueConstraintWriteFailure.AuthorityMismatch);
    }

    [Fact]
    public async Task should_compare_the_epoch()
    {
        await Seed();
        var wrong = _authority with { Epoch = new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")) };
        ShouldRefuse(await _storage.TryTransition(_definition.Name, wrong, Run(1)), UniqueConstraintWriteFailure.AuthorityMismatch);
        await ShouldRetainClaim();
    }

    [Fact]
    public async Task should_compare_the_run_identifier()
    {
        var expected = _authority with { Phase = Run(1) };
        (await _storage.InitializeAuthority(_definition.Name, expected)).IsSuccess.ShouldBeTrue();
        ShouldRefuse(await _storage.TryTransition(_definition.Name, expected with { Phase = Run(2) }, UniqueConstraintWritePhase.Live), UniqueConstraintWriteFailure.AuthorityMismatch);
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)expected);
    }

    [Fact]
    public async Task should_compare_the_revision()
    {
        await Initialize();
        ShouldRefuse(await _storage.TryTransition(_definition.Name, _authority with { Revision = new(1) }, Run(1)), UniqueConstraintWriteFailure.AuthorityMismatch);
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)_authority);
    }

    [Fact]
    public async Task should_refuse_revision_exhaustion_without_changing_claims()
    {
        var exhausted = _authority with { Revision = new(long.MaxValue) };
        (await _storage.InitializeAuthority(_definition.Name, exhausted)).IsSuccess.ShouldBeTrue();
        (await _storage.Save(_owner, _definition, 10, _value, _scope, exhausted, _subject)).IsSuccess.ShouldBeTrue();
        ShouldRefuse(await _storage.TryTransition(_definition.Name, exhausted, Run(1)), UniqueConstraintWriteFailure.RevisionExhausted);
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)exhausted);
        await ShouldRetainClaim();
    }

    [Fact]
    public Task should_fence_save_with_a_stale_proof() => RefuseStaleMutation(0);

    [Fact]
    public Task should_fence_remove_with_a_stale_proof() => RefuseStaleMutation(1);

    [Fact]
    public Task should_fence_remove_value_with_a_stale_proof() => RefuseStaleMutation(2);

    [Fact]
    public Task should_fence_clear_values_with_a_stale_proof() => RefuseStaleMutation(3);

    [Fact]
    public Task should_fence_remove_all_with_a_stale_proof() => RefuseStaleMutation(4);

    [Fact]
    public Task should_fence_subject_removal_with_a_stale_proof() => RefuseStaleMutation(5);

    async Task RefuseStaleMutation(int operation)
    {
        await Seed();
        (await _storage.Save(_owner, PerValue, 10, _value, _scope, _authority, _subject)).IsSuccess.ShouldBeTrue();
        var current = await _storage.TryTransition(_definition.Name, _authority, Run(1));
        current.IsSuccess.ShouldBeTrue();
        ShouldRefuse(await Mutation(operation, _authority), UniqueConstraintWriteFailure.AuthorityMismatch);
        await ShouldRetainClaim();
        await ShouldRetainClaim(PerValue);
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)current.AsT0);
    }
}
