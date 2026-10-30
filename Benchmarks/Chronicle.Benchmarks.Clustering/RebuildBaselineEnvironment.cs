// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>Collects process environment and assembly version metadata.</summary>
internal static class RebuildBaselineEnvironment
{
    /// <summary>Gets the running kernel informational version.</summary>
    internal static string? KernelVersion => typeof(KernelCore::Cratis.Chronicle.Observation.IObserverService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    /// <summary>Gets the running Cratis.Orleans informational version.</summary>
    internal static string? OrleansVersion => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "Cratis.Orleans")?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    /// <summary>Reports OS, CPU count and process-available memory.</summary>
    /// <returns>The machine metadata and the boundary of its memory measurement.</returns>
    internal static object Machine() => new
    {
        OS = RuntimeInformation.OSDescription,
        CpuCount = Environment.ProcessorCount,
        RamBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
        RamBoundary = "Memory available to this process (including container limits)"
    };

    /// <summary>Resolves the commit from the environment or a bounded Git query.</summary>
    /// <returns>A task containing the commit identifier, or unknown when it cannot be resolved.</returns>
    internal static async Task<string> Commit()
    {
        if ((Environment.GetEnvironmentVariable("GITHUB_SHA") ?? Environment.GetEnvironmentVariable("COMMIT_SHA")) is { } value) return value;
        try
        {
            using var git = Process.Start(new ProcessStartInfo("git", "rev-parse HEAD") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true });
            if (git is null) return "unknown";
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var output = await git.StandardOutput.ReadToEndAsync(timeout.Token);
            await git.WaitForExitAsync(timeout.Token);
            return git.ExitCode == 0 ? output.Trim() : "unknown";
        }
        catch (Exception)
        {
            return "unknown";
        }
    }
}
