// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Owns a restartable workload child process and its output streams.
/// </summary>
/// <param name="role">The producer or consumer role.</param>
/// <param name="environment">The dedicated run environment.</param>
/// <param name="store">The event store name.</param>
/// <param name="output">The output directory.</param>
internal sealed class ChildWorker(string role, DockerEnvironment environment, string store, string output) : IAsyncDisposable
{
    Process? _process;
    Task _stdout = Task.CompletedTask;
    Task _stderr = Task.CompletedTask;
    int _generation;
    int? _stoppedProcess;
    CancellationTokenSource? _outputCancellation;

    /// <summary>
    /// Gets whether the child process is running.
    /// </summary>
    public bool IsRunning => _stoppedProcess is not null || _process is { HasExited: false };

    /// <summary>
    /// Starts the child and waits for its readiness marker.
    /// </summary>
    /// <param name="cancellation">The startup cancellation token.</param>
    /// <returns>The startup task.</returns>
    /// <exception cref="InvalidOperationException">The process could not start or exited before readiness.</exception>
    public async Task Start(CancellationToken cancellation)
    {
        if (IsRunning) return;
        if (_process is not null) await Finish();
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { typeof(ChildWorker).Assembly.Location, role, environment.Connection, store, environment.LedgerConnection, environment.RunId }) start.ArgumentList.Add(argument);
        _process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {role}.");
        _generation++;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _outputCancellation = new();
        _stdout = Pump(_process.StandardOutput, Path.Combine(output, $"{role}-{_generation}.stdout.log"), ready, _outputCancellation.Token);
        _stderr = Pump(_process.StandardError, Path.Combine(output, $"{role}-{_generation}.stderr.log"), null, _outputCancellation.Token);
        var exit = _process.WaitForExitAsync(cancellation);
        var completed = await Task.WhenAny(ready.Task, exit).WaitAsync(TimeSpan.FromSeconds(100), cancellation);
        if (completed == exit) throw new InvalidOperationException($"{role} exited before READY with code {_process.ExitCode}.");
        await ready.Task;
    }

    /// <summary>
    /// Sends a signal to the running child.
    /// </summary>
    /// <param name="signal">The signal name.</param>
    /// <param name="cancellation">The command cancellation token.</param>
    /// <returns>The signal task.</returns>
    /// <exception cref="InvalidOperationException">The child is not running.</exception>
    public Task Signal(string signal, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (signal == "CONT")
        {
            Resume();
            return Task.CompletedTask;
        }
        if (_process is not { HasExited: false }) throw new InvalidOperationException($"{role} is not running.");
        _stoppedProcess = _process.Id;
        Console.WriteLine($"SIGNAL {role} pid={_stoppedProcess} {signal}");
        NativeSignals.Send(_stoppedProcess.Value, signal);
        Console.WriteLine($"SIGNAL {role} {signal} delivered");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resumes an owned stopped child even when the run token is already canceled.
    /// </summary>
    public void Resume()
    {
        if (_stoppedProcess is not { } processId) return;
        NativeSignals.Send(processId, "CONT");
        _stoppedProcess = null;
        Console.WriteLine($"SIGNAL {role} pid={processId} CONT delivered");
    }

    /// <summary>
    /// Kills the child and drains its output streams.
    /// </summary>
    /// <returns>The termination task.</returns>
    public async Task Kill()
    {
        Resume();
        if (_process is { HasExited: false }) _process.Kill(entireProcessTree: true);
        await Finish();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await Kill();

    static async Task Pump(StreamReader reader, string path, TaskCompletionSource? ready, CancellationToken cancellation)
    {
        try
        {
            await using var file = new StreamWriter(path) { AutoFlush = true };
            while (await reader.ReadLineAsync(cancellation) is { } line)
            {
                await file.WriteLineAsync(line.AsMemory(), cancellation);
                if (line == "READY") ready?.TrySetResult();
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
    }

    async Task Finish()
    {
        if (_process is null) return;
        try
        {
            await _process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));
            await Task.WhenAll(_stdout, _stderr).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException exception)
        {
            throw new TimeoutException($"Timed out waiting for {role} pid={_process.Id} exit/output drain.", exception);
        }
        finally
        {
            try
            {
                if (_outputCancellation is not null) await _outputCancellation.CancelAsync().WaitAsync(TimeSpan.FromSeconds(2));
            }
            finally
            {
                _outputCancellation?.Dispose();
                _outputCancellation = null;
                _process.Dispose();
                _process = null;
            }
        }
    }
}
