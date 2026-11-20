// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cratis.Chronicle.Benchmarks.Storage.Hosts;

/// <summary>
/// Best-effort facts about the machine, Docker and the repository that go into the settings ledger.
/// Every lookup is allowed to fail: the ledger then says `unknown` instead of guessing.
/// </summary>
internal static class HostFacts
{
    /// <summary>The value written when a fact cannot be found.</summary>
    internal const string Unknown = "unknown";

    /// <summary>Runs a process and returns its trimmed standard output, or <see langword="null"/> when it fails or times out.</summary>
    /// <param name="fileName">The executable.</param>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The output or <see langword="null"/>.</returns>
    public static string? Run(string fileName, params string[] arguments)
    {
        try
        {
            var info = new ProcessStartInfo(fileName) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            foreach (var argument in arguments) info.ArgumentList.Add(argument);
            using var process = Process.Start(info);
            if (process is null) return null;
            var output = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit(15_000))
            {
                process.Kill();
                return null;
            }

            return process.ExitCode == 0 ? output.GetAwaiter().GetResult().Trim() : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Gets the Chronicle commit the run was built from.</summary>
    /// <returns>The commit hash, or `unknown`.</returns>
    public static string Commit() => Run("git", "rev-parse", "HEAD") ?? Environment.GetEnvironmentVariable("GITHUB_SHA") ?? Unknown;

    /// <summary>Gets the digest of a locally present image.</summary>
    /// <param name="image">The image tag.</param>
    /// <returns>The digest, or `unknown`.</returns>
    public static string ImageDigest(string image) => Run("docker", "image", "inspect", "--format", "{{if .RepoDigests}}{{index .RepoDigests 0}}{{else}}{{.Id}}{{end}}", image) ?? Unknown;

    /// <summary>Describes the hardware, including what Docker exposes.</summary>
    /// <returns>The description.</returns>
    public static HardwareDescription Hardware()
    {
        long? dockerMemory = null;
        int? dockerCpus = null;
        var info = Run("docker", "info", "--format", "{{.MemTotal}} {{.NCPU}}")?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (info is { Length: 2 })
        {
            if (long.TryParse(info[0], out var memory)) dockerMemory = memory;
            if (int.TryParse(info[1], out var cpus)) dockerCpus = cpus;
        }

        return new(
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            Environment.ProcessorCount,
            GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            dockerMemory,
            dockerCpus);
    }

    /// <summary>Builds a ledger from the facts a provider host gathered.</summary>
    /// <param name="provider">The provider.</param>
    /// <param name="options">The run options.</param>
    /// <param name="loadAtStart">The machine load at the start.</param>
    /// <param name="image">The image tag, or a description for a file-backed provider.</param>
    /// <param name="serverVersion">The server version.</param>
    /// <param name="durability">The durability settings read from the server.</param>
    /// <param name="durabilityClass">The durability class.</param>
    /// <param name="connectionPoolSize">The pool size in effect.</param>
    /// <param name="digest">The image digest.</param>
    /// <returns>The ledger.</returns>
    public static SettingsLedger Ledger(
        StorageProvider provider,
        StorageRunOptions options,
        LoadReading loadAtStart,
        string image,
        string serverVersion,
        IReadOnlyDictionary<string, string> durability,
        string durabilityClass,
        int connectionPoolSize,
        string digest) => new(
            StorageProviderNames.NameOf(provider),
            options.Profile.Name,
            image,
            digest,
            serverVersion,
            provider == StorageProvider.Mongo ? $"single-node replica set ({options.MongoTopology})" : "single node",
            durability,
            durabilityClass,
            Commit(),
            options.Profile.BatchSizes,
            options.Profile.Concurrency,
            options.Profile.PayloadBytes,
            options.Profile.EventCounts,
            connectionPoolSize,
            0,
            true,
            Hardware(),
            loadAtStart,
            null,
            MachineLoad.Threshold(Environment.ProcessorCount, options.MaxLoad),
            DateTimeOffset.UtcNow);
}
