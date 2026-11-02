// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;

namespace Cratis.Chronicle.Integration.for_PerProcessActivation;

public sealed class ChildProcess : IAsyncDisposable
{
    readonly Process _process;
    readonly ProtocolOutput _output = new();
    readonly StringBuilder _errors = new();
    readonly Task _stdout;
    readonly Task _stderr;
    int _disposed;

    ChildProcess(Process process)
    {
        _process = process;
        _stdout = ReadOutput();
        _stderr = ReadErrors();
    }

    public static ChildProcess Start(string connection, string store, string verb = PerProcessApplication.Verb)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                ArgumentList = { typeof(ChildProcess).Assembly.Location, verb, "--connection", connection, "--event-store", store },
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        try
        {
            if (!process.Start()) throw new InvalidOperationException("Could not start the per-process application.");
            return new ChildProcess(process);
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    public async Task<string> WaitForLine(string prefix, TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        try
        {
            if (await _output.WaitForLine(prefix, deadline.Token) is { } line) return line;
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            throw new TimeoutException($"Child did not report {prefix} within {timeout}. {Diagnostics()}");
        }
        throw new InvalidOperationException($"Child stdout ended before {prefix}. {Diagnostics()}");
    }

    public async Task Send(string command)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await _process.StandardInput.WriteLineAsync(command.AsMemory(), deadline.Token);
        await _process.StandardInput.FlushAsync(deadline.Token);
    }

    public async Task Kill()
    {
        Assert.False(_process.HasExited, Diagnostics());
        _process.Kill(entireProcessTree: true);
        await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
        await Task.WhenAll(_stdout, _stderr).WaitAsync(TimeSpan.FromSeconds(10));
        _process.HasExited.ShouldBeTrue();
        _process.ExitCode.ShouldNotEqual(0);
    }

    public async Task Stop()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await _process.StandardInput.WriteLineAsync("stop".AsMemory(), deadline.Token);
        _process.StandardInput.Close();
        await _process.WaitForExitAsync(deadline.Token);
        await Task.WhenAll(_stdout, _stderr).WaitAsync(deadline.Token);
        Assert.True(_process.ExitCode == 0, Diagnostics());
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        try
        {
            if (!_process.HasExited) _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            await Task.WhenAll(_stdout, _stderr).WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            _process.Dispose();
        }
    }

    async Task ReadOutput()
    {
        try
        {
            while (await _process.StandardOutput.ReadLineAsync() is { } line) _output.Add(line);
            _output.Complete();
        }
        catch (Exception exception)
        {
            _output.Complete(exception);
            throw;
        }
    }

    async Task ReadErrors()
    {
        while (await _process.StandardError.ReadLineAsync() is { } line)
        {
            lock (_errors) _errors.AppendLine(line);
        }
    }

    string Diagnostics()
    {
        var state = _process.HasExited ? $"exited {_process.ExitCode}" : "still running";
        lock (_errors) return $"Child {_process.Id} {state}. Stderr: {_errors}";
    }
}
