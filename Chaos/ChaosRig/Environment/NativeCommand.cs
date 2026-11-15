// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Executes an owned process while preserving output and failure status.
/// </summary>
internal static class NativeCommand
{
    /// <summary>
    /// Runs a command with a thirty-second limit and bounded termination on cancellation.
    /// </summary>
    /// <param name="executable">The executable.</param>
    /// <param name="arguments">Arguments passed without shell expansion.</param>
    /// <param name="cancellation">Execution cancellation.</param>
    /// <returns>Standard output on success.</returns>
    /// <exception cref="InvalidOperationException">The command could not start or exited unsuccessfully.</exception>
    /// <exception cref="TimeoutException">The command exceeded its control-operation deadline.</exception>
    public static async Task<string> Run(string executable, IEnumerable<string> arguments, CancellationToken cancellation)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {executable}.");
        var output = process.StandardOutput.ReadToEndAsync(deadline.Token);
        var error = process.StandardError.ReadToEndAsync(deadline.Token);
        try
        {
            await process.WaitForExitAsync(deadline.Token).WaitAsync(deadline.Token);
            await Task.WhenAll(output, error).WaitAsync(deadline.Token);
            if (process.ExitCode != 0) throw new InvalidOperationException($"{executable} exited {process.ExitCode}: {await error}");
            return await output;
        }
        catch (OperationCanceledException exception) when (!cancellation.IsCancellationRequested)
        {
            throw new TimeoutException($"Control command {executable} pid={process.Id} exceeded 30 seconds.", exception);
        }
        finally
        {
            await Terminate(process, executable);
        }
    }

    static async Task Terminate(Process process, string executable)
    {
        try
        {
            if (process.HasExited) return;
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync($"Control command cleanup failed for {executable} pid={process.Id}: {exception.Message}");
        }
    }
}
