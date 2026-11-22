// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

public abstract partial class UniqueConstraintsStorageV2Contract
{
    [Fact]
    public Task should_compare_epoch_for_all_mutations() => RefuseEveryMutation(_authority with { Epoch = new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")) });

    [Fact]
    public Task should_compare_phase_for_all_mutations() => RefuseEveryMutation(_authority with { Phase = Run(1) });

    [Fact]
    public Task should_compare_revision_for_all_mutations() => RefuseEveryMutation(_authority with { Revision = new(1) });

    [Fact]
    public async Task should_compare_run_for_all_mutations()
    {
        await Seed();
        (await _storage.TryTransition(_definition.Name, _authority, Run(1))).IsSuccess.ShouldBeTrue();
        var wrong = _authority with { Phase = Run(2), Revision = new(1) };
        for (var operation = 0; operation < 6; operation++)
        {
            ShouldRefuse(await Mutation(operation, wrong), UniqueConstraintWriteFailure.AuthorityMismatch);
        }

        await ShouldRetainClaim();
    }

    async Task RefuseEveryMutation(UniqueConstraintWriteAuthority wrong)
    {
        await Seed();
        (await _storage.Save(_owner, PerValue, 10, _value, _scope, _authority, _subject)).IsSuccess.ShouldBeTrue();
        for (var operation = 0; operation < 6; operation++)
        {
            ShouldRefuse(await Mutation(operation, wrong), UniqueConstraintWriteFailure.AuthorityMismatch);
        }

        await ShouldRetainClaim();
        await ShouldRetainClaim(PerValue);
        await ShouldRetainAuthority();
    }
}
