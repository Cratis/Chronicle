// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public abstract partial class UniqueConstraintsStorageV2Contract
{
    [Fact]
    public async Task should_have_one_winner_for_competing_initializers()
    {
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var proposed = new[] { _authority, _authority with { Epoch = new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")) } };
        var attempts = proposed.Select(async proof =>
        {
            await start.Task;
            return await _storage.InitializeAuthority(_definition.Name, proof);
        }).ToArray();
        start.SetResult();
        var results = await Task.WhenAll(attempts).WaitAsync(TimeSpan.FromSeconds(30));
        results.Count(result => result.IsSuccess).ShouldEqual(1);
        var winner = results.Single(result => result.IsSuccess).AsT0;
        ShouldRefuse(results.Single(result => !result.IsSuccess), UniqueConstraintWriteFailure.AuthorityMismatch);
        _storage = await ReopenStorage();
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)winner);
    }

    [Fact]
    public async Task should_have_one_winner_for_competing_transitions()
    {
        await Seed();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = Enumerable.Range(1, 2).Select(async run =>
        {
            await start.Task;
            return await _storage.TryTransition(_definition.Name, _authority, Run(run));
        }).ToArray();
        start.SetResult();
        var results = await Task.WhenAll(attempts).WaitAsync(TimeSpan.FromSeconds(30));
        results.Count(result => result.IsSuccess).ShouldEqual(1);
        ShouldRefuse(results.Single(result => !result.IsSuccess), UniqueConstraintWriteFailure.AuthorityMismatch);
        _storage = await ReopenStorage();
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)results.Single(result => result.IsSuccess).AsT0);
        await ShouldRetainClaim();
    }

    [Fact]
    public Task should_fence_save_after_a_transition_passes_its_precheck() => InterleavedMutation(0, true);

    [Fact]
    public Task should_fence_remove_after_a_transition_passes_its_precheck() => InterleavedMutation(1, true);

    [Fact]
    public Task should_fence_remove_value_after_a_transition_passes_its_precheck() => InterleavedMutation(2, true);

    [Fact]
    public Task should_fence_clear_after_a_transition_passes_its_precheck() => InterleavedMutation(3, true);

    [Fact]
    public Task should_fence_remove_all_after_a_transition_passes_its_precheck() => InterleavedMutation(4, true);

    [Fact]
    public Task should_fence_subject_removal_after_a_transition_passes_its_precheck() => InterleavedMutation(5, true);

    [Fact]
    public Task should_commit_save_before_a_waiting_transition() => InterleavedMutation(0, false);

    [Fact]
    public Task should_commit_remove_before_a_waiting_transition() => InterleavedMutation(1, false);

    [Fact]
    public Task should_commit_remove_value_before_a_waiting_transition() => InterleavedMutation(2, false);

    [Fact]
    public Task should_commit_clear_before_a_waiting_transition() => InterleavedMutation(3, false);

    [Fact]
    public Task should_commit_remove_all_before_a_waiting_transition() => InterleavedMutation(4, false);

    [Fact]
    public Task should_commit_subject_removal_before_a_waiting_transition() => InterleavedMutation(5, false);

    async Task InterleavedMutation(int operation, bool transitionFirst)
    {
        await Seed();
        (await _storage.Save(_owner, PerValue, 10, _value, _scope, _authority, _subject)).IsSuccess.ShouldBeTrue();
        UniqueConstraintWriteAuthority? next = null;
        var result = await Interleave(async () =>
        {
            var transitioned = await _storage.TryTransition(_definition.Name, _authority, Run(1));
            transitioned.IsSuccess.ShouldBeTrue();
            next = transitioned.AsT0;
        }, () => Mutation(operation, _authority), transitionFirst);
        _storage = await ReopenStorage();
        (await _storage.GetAuthority(_definition.Name)).ShouldEqual((Option<UniqueConstraintWriteAuthority>)next!);
        if (transitionFirst)
        {
            ShouldRefuse(result, UniqueConstraintWriteFailure.AuthorityMismatch);
            await ShouldRetainClaim();
            await ShouldRetainClaim(PerValue);
            return;
        }

        result.IsSuccess.ShouldBeTrue();
        var expected = operation == 0 ? new[] { _otherValue } : operation == 2 ? [_value] : [];
        (await _storage.GetFor(_owner, _definition, _scope)).SequenceEqual(expected).ShouldBeTrue();
        (await _storage.GetFor(_owner, PerValue, _scope)).SequenceEqual(operation == 2 ? [] : [_value]).ShouldBeTrue();
    }
}
