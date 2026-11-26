// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Server.for_KernelHost.given;

public class a_startup_diagnostic : Specification
{
    internal manual_startup_clock Clock;
    internal held_active_stage Hold;
    internal diagnostic_silo_lifecycle Lifecycle;
    internal Task Startup;
    internal TaskCompletionSource Blocked;
    internal Exception Error;
    internal string Message;

    void Establish()
    {
        Clock = new();
        Hold = new(Clock);
        Lifecycle = new(NullLogger<SiloLifecycleSubject>.Instance, Substitute.For<ILocalSiloDetails>(), Hold.Diagnostics);
        Hold.Participate(Lifecycle);
        Startup = Task.CompletedTask;
        Blocked = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    internal async Task CaptureTimeout()
    {
        Hold.Diagnostics.ObserveStartup(Startup);
        var waiting = Hold.Diagnostics.WaitForActive(Startup, Hold, CancellationToken.None);
        Clock.Expire();
        Error = await Catch.Exception(() => waiting);
        Message = Error.Message;
    }

    async Task Destroy()
    {
        Hold.Release.TrySetResult();
        Blocked.TrySetResult();
        if (!Startup.IsFaulted) await Startup;
    }

    internal sealed class manual_startup_clock : TimeProvider
    {
        Action _expire = () => { };
        long _elapsed;

        internal TimeSpan DueTime { get; private set; }
        internal int TimerCount { get; private set; }

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _elapsed;
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(_elapsed);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            DueTime = dueTime;
            TimerCount++;
            _expire = () => callback(state);
            return Substitute.For<ITimer>();
        }

        internal void Advance(TimeSpan duration) => _elapsed += duration.Ticks;

        internal void Expire()
        {
            Advance(DueTime);
            _expire();
        }
    }
}
