// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.when_stopping;

public class and_the_lease_was_held : given.a_keeper
{
    async Task Because()
    {
        await Observer.OnStart(CancellationToken.None);
        Time.Advance(TimeSpan.FromSeconds(3));
        await Observer.OnStop(CancellationToken.None);
    }

    [Fact] void should_report_nothing_held() => Keeper.HeldFor.ShouldEqual(TimeSpan.Zero);
}
