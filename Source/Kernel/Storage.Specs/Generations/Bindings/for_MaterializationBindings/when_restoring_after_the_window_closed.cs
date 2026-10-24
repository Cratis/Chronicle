// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Generations.Bindings.for_MaterializationBindings;

public abstract class when_restoring_after_the_window_closed<THarness> : given.a_published_materialization<THarness>
    where THarness : IMaterializationBindingStorageHarness, new()
{
    MaterializationPublication _atDeadline;
    MaterializationPublication _afterDeadline;

    async Task Because()
    {
        _time.Advance(_rollbackWindow - TimeSpan.FromMinutes(10));
        _atDeadline = await Restore();
        _time.Advance(TimeSpan.FromTicks(1));
        _restoreReceipt = new(Guid.NewGuid());
        _afterDeadline = await Restore();
    }

    [Fact] public void should_refuse_at_the_deadline() => _atDeadline.Result.ShouldEqual(MaterializationPublicationResult.CandidateNotRestorable);
    [Fact] public void should_refuse_after_the_deadline() => _afterDeadline.Result.ShouldEqual(MaterializationPublicationResult.CandidateNotRestorable);
    [Fact] public Task should_not_change_the_binding() => ShouldKeepBindingUnchanged();
}
