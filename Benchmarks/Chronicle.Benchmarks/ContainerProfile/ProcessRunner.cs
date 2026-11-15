// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Executes bounded child processes and kills their tree on cancellation.</summary>
internal static class ProcessRunner
{
    /// <summary>Runs one command and drains both redirected streams.</summary>
    /// <param name="executable">The executable.</param>
    /// <param name="arguments">Its argument vector.</param>
    /// <param name="deadline">The execution limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The process outcome.</returns>
    public static async Task<ProcessResult> Run(string executable, IEnumerable<string> arguments, TimeSpan deadline, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(deadline);
        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        using var process = new Process { StartInfo = start };
        var watch = Stopwatch.StartNew();
        process.Start();
        var stdout = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(stdout, stderr);
            cancellationToken.ThrowIfCancellationRequested();
            return new ProcessResult(124, string.Empty, "Deadline exceeded", watch.Elapsed.TotalMilliseconds);
        }
        return new ProcessResult(process.ExitCode, await stdout, await stderr, watch.Elapsed.TotalMilliseconds);
    }
}
