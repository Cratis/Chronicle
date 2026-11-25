// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelHost;

public class when_the_active_hold_is_entered_before_waiting : given.a_startup_diagnostic
{
    void Establish()
    {
        Startup = Lifecycle.OnStart();
        Hold.Diagnostics.ObserveStartup(Startup);
    }

    async Task Because()
    {
        Error = await Catch.Exception(() => Hold.Diagnostics.WaitForActive(Startup, Hold, CancellationToken.None));
        Clock.Advance(TimeSpan.FromMilliseconds(50));
        Hold.Release.TrySetResult();
        await Startup;
        Message = Hold.Diagnostics.Describe(Startup, Hold, CancellationToken.None);
    }

    [Fact] void should_not_lose_the_entry_signal() => Error.ShouldBeNull();
    [Fact] void should_not_arm_a_deadline_for_an_already_entered_hold() => Clock.TimerCount.ShouldEqual(0);
    [Fact] void should_capture_the_released_hold() => Message.ShouldContain("hold armed=True, entered=True, released=True");
    [Fact] void should_capture_the_active_stage() => Message.ShouldContain($"lifecycle stage {nameof(ServiceLifecycleStage.Active)}");
    [Fact] void should_timestamp_hold_entry() => Message.ShouldContain("Active hold entered at UTC=2026-01-01T00:00:00.0000000+00:00");
    [Fact] void should_timestamp_hold_release() => Message.ShouldContain("Active hold released at UTC=2026-01-01T00:00:00.0500000+00:00");
    [Fact] void should_capture_completed_startup() => Message.ShouldContain("hostStart=RanToCompletion");
}
