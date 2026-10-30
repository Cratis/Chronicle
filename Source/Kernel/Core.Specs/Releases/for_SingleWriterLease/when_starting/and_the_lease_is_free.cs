// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_starting;

public class and_the_lease_is_free : given.a_keeper
{
    async Task Because()
    {
        await Observer.OnStart(CancellationToken.None);
    }

    [Fact] void should_hold_the_lease() => Keeper.IsHeld.ShouldBeTrue();
    [Fact] void should_start_before_runtime_initialization() => Lifecycle.Received().Subscribe(nameof(SingleWriterLeaseKeeper), ServiceLifecycleStage.RuntimeInitialize - 1, Arg.Any<ILifecycleObserver>());
    [Fact] void should_use_a_unique_owner() => Keeper.Owner.ShouldNotEqual(SingleWriterLeaseOwner.New());
}
