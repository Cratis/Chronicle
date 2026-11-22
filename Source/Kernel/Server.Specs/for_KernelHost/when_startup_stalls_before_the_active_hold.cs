// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelHost;

public class when_startup_stalls_before_the_active_hold : given.a_startup_diagnostic
{
    const int CustomStage = ServiceLifecycleStage.RuntimeInitialize + 1;

    void Establish()
    {
        Lifecycle.Subscribe("initializing", ServiceLifecycleStage.RuntimeInitialize, _ => Task.CompletedTask);
        Lifecycle.Subscribe("blocked", CustomStage, _ => Blocked.Task);
        Clock.Advance(TimeSpan.FromMilliseconds(100));
        Hold.Diagnostics.Record("host StartAsync invoked");
        Startup = Lifecycle.OnStart();
    }

    Task Because() => CaptureTimeout();

    [Fact] void should_still_throw_a_timeout() => Error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_keep_the_ten_second_deadline() => Clock.DueTime.ShouldEqual(TimeSpan.FromSeconds(10));
    [Fact] void should_name_the_wait() => Message.ShouldContain("Timed out waiting for Orleans Active hold entry (timeout=10000 ms)");
    [Fact] void should_capture_the_last_actual_stage_and_its_timestamp() => Message.ShouldContain($"({CustomStage}) entered at UTC=2026-01-01T00:00:00.1000000+00:00");
    [Fact] void should_capture_named_stages() => Message.ShouldContain(nameof(ServiceLifecycleStage.RuntimeInitialize));
    [Fact] void should_capture_total_elapsed_time() => Message.ShouldContain("elapsedMs=10100.000");
    [Fact] void should_capture_time_since_the_last_step() => Message.ShouldContain("currentStepMs=10000.000");
    [Fact] void should_capture_elapsed_time_per_step() => Message.ShouldContain("sincePreviousMs=100.000");
    [Fact] void should_capture_the_armed_but_unentered_hold() => Message.ShouldContain("hold armed=True, entered=False, released=False");
    [Fact] void should_capture_pending_startup() => Message.ShouldContain("hostStart=WaitingForActivation");
    [Fact] void should_distinguish_no_startup_exception() => Message.ShouldContain("startupException=none");
    [Fact] void should_capture_startup_cancellation_state() => Message.ShouldContain("startupCancellationRequested=False");
}
