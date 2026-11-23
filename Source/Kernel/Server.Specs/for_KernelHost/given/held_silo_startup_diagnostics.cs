// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;

namespace Cratis.Chronicle.Server.for_KernelHost.given;

internal sealed class held_silo_startup_diagnostics(TimeProvider clock)
{
    const int MaximumSteps = 64;
    readonly long _started = clock.GetTimestamp();
    readonly List<startup_step> _steps = [];
    startup_step? _lastStage;
    startup_step? _lastStep;
    int _omittedSteps;

    internal void Record(string name)
    {
        lock (_steps) Append(name);
    }

    internal void StageReached(int stage, string name)
    {
        lock (_steps) _lastStage = Append($"lifecycle stage {name} ({stage}) entered");
    }

    internal void ObserveStartup(Task startup)
    {
        Record("host StartAsync returned");
        _ = startup.ContinueWith(
            completed => Record($"host startup completed: {completed.Status}"),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    internal async Task WaitForActive(Task startup, held_active_stage hold, CancellationToken startupToken)
    {
        Record("waiting for Orleans Active hold entry");
        try
        {
            await hold.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), clock);
        }
        catch (TimeoutException exception)
        {
            throw new TimeoutException($"Timed out waiting for Orleans Active hold entry (timeout=10000 ms). {Describe(startup, hold, startupToken)}", exception);
        }
    }

    internal string Describe(Task startup, held_active_stage hold, CancellationToken startupToken)
    {
        lock (_steps)
        {
            var elapsed = clock.GetElapsedTime(_started);
            var currentStep = elapsed - (_lastStep?.Elapsed ?? TimeSpan.Zero);
            var timing = string.Create(CultureInfo.InvariantCulture, $"Snapshot UTC={clock.GetUtcNow():O}; elapsedMs={elapsed.TotalMilliseconds:F3}; currentStepMs={currentStep.TotalMilliseconds:F3}");
            return $"{timing}; lastLifecycleStage={Format(_lastStage)}; " +
                $"hold armed={hold.Armed}, entered={hold.Entered.Task.IsCompleted}, released={hold.Release.Task.IsCompleted}; " +
                $"hostStart={startup.Status}; startupCancellationRequested={startupToken.IsCancellationRequested}; " +
                $"steps=[{string.Join("; ", _steps.Select(Format))}]; omittedSteps={_omittedSteps}; startupException={DescribeException(startup.Exception)}";
        }
    }

    startup_step Append(string name)
    {
        var elapsed = clock.GetElapsedTime(_started);
        var step = new startup_step(name[..Math.Min(name.Length, 128)], clock.GetUtcNow(), elapsed, elapsed - (_lastStep?.Elapsed ?? TimeSpan.Zero));
        _lastStep = step;
        if (_steps.Count < MaximumSteps) _steps.Add(step);
        else _omittedSteps++;
        return step;
    }

    static string Format(startup_step? step) => step is null ? "none observed" : string.Create(CultureInfo.InvariantCulture,
        $"{step.Name} at UTC={step.Utc:O}, elapsedMs={step.Elapsed.TotalMilliseconds:F3}, sincePreviousMs={step.SincePrevious.TotalMilliseconds:F3}");

    static string DescribeException(Exception? exception)
    {
        var errors = new List<string>();
        for (var depth = 0; exception is not null && depth < 4; depth++, exception = exception.InnerException)
        {
            var socket = exception is SocketException socketException ? $", SocketError={socketException.SocketErrorCode}" : string.Empty;
            var frames = new StackTrace(exception, false).GetFrames().Take(3)
                .Select(frame => $"{frame.GetMethod()?.DeclaringType?.FullName}.{frame.GetMethod()?.Name}");
            var description = $"{exception.GetType().FullName} (HResult=0x{exception.HResult.ToString("X8", CultureInfo.InvariantCulture)}{socket}) [{string.Join(", ", frames)}]";
            errors.Add(description[..Math.Min(description.Length, 1024)]);
        }

        // Never emit exception messages, Data, paths, or configuration: these can contain credentials.
        return errors.Count == 0 ? "none" : $"{string.Join(" -> ", errors)} (up to 4 exceptions/3 frames each; messages and data omitted)";
    }

    sealed record startup_step(string Name, DateTimeOffset Utc, TimeSpan Elapsed, TimeSpan SincePrevious);
}
