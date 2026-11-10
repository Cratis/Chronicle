// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Chaos;

internal sealed class ChildWorker(string role, DockerEnvironment environment, string store, string output) : IAsyncDisposable
{
    Process? _process;
    Task _stdout = Task.CompletedTask;
    Task _stderr = Task.CompletedTask;
    int _generation;

    public bool IsRunning => _process is { HasExited: false };

    public async Task Start(CancellationToken cancellation)
    {
        if (IsRunning) return;
        if (_process is not null) await Finish();
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { typeof(ChildWorker).Assembly.Location, role, environment.Connection, store, environment.LedgerConnection, environment.RunId }) start.ArgumentList.Add(argument);
        _process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {role}.");
        _generation++;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _stdout = Pump(_process.StandardOutput, Path.Combine(output, $"{role}-{_generation}.stdout.log"), ready);
        _stderr = Pump(_process.StandardError, Path.Combine(output, $"{role}-{_generation}.stderr.log"), null);
        var exit = _process.WaitForExitAsync(cancellation);
        var completed = await Task.WhenAny(ready.Task, exit).WaitAsync(TimeSpan.FromSeconds(100), cancellation);
        if (completed == exit) throw new InvalidOperationException($"{role} exited before READY with code {_process.ExitCode}.");
        await ready.Task;
    }

    public async Task Signal(string signal, CancellationToken cancellation)
    {
        if (_process is not { HasExited: false }) throw new InvalidOperationException($"{role} is not running.");
        await NativeCommand.Run("kill", [$"-{signal}", _process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)], cancellation);
    }

    public async Task Kill()
    {
        if (_process is { HasExited: false }) _process.Kill(entireProcessTree: true);
        await Finish();
    }

    public async ValueTask DisposeAsync() => await Kill();

    async Task Finish()
    {
        if (_process is null) return;
        await _process.WaitForExitAsync(CancellationToken.None);
        await Task.WhenAll(_stdout, _stderr);
        _process.Dispose();
        _process = null;
    }

    static async Task Pump(StreamReader reader, string path, TaskCompletionSource? ready)
    {
        await using var file = new StreamWriter(path) { AutoFlush = true };
        while (await reader.ReadLineAsync() is { } line)
        {
            await file.WriteLineAsync(line);
            if (line == "READY") ready?.TrySetResult();
        }
    }
}
