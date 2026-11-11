// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_starting;

public class and_the_lease_has_been_held_for_a_while : given.a_keeper
{
    TimeSpan _heldForBeforeAcquiring;

    async Task Because()
    {
        _heldForBeforeAcquiring = Keeper.HeldFor;
        await Observer.OnStart(CancellationToken.None);
        Time.Advance(TimeSpan.FromSeconds(3));
    }

    [Fact] void should_report_nothing_held_before_acquiring() => _heldForBeforeAcquiring.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_report_how_long_it_has_been_held() => Keeper.HeldFor.ShouldEqual(TimeSpan.FromSeconds(3));
}
