// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Globalization;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Reads the machine load and decides whether a measurement may start.</summary>
public static class MachineLoad
{
    /// <summary>The process exit code used when the machine is too loaded to measure.</summary>
    public const int RefusedExitCode = 3;

    /// <summary>The share of the CPU count the one minute load average may reach before a run is refused.</summary>
    public const double DefaultLoadFactor = 0.7;

    /// <summary>Parses either `/proc/loadavg` (`0.50 0.40 0.30 1/200 1234`) or `sysctl vm.loadavg` (`{ 0.50 0.40 0.30 }`).</summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The reading, or <see cref="LoadReading.Unknown"/> when the text carries no three numbers.</returns>
    public static LoadReading Parse(string text)
    {
        var numbers = text
            .Split([' ', '{', '}', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(token => double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? (double?)value : null)
            .TakeWhile(value => value is not null)
            .Select(value => value!.Value)
            .Take(3)
            .ToArray();
        return numbers.Length == 3 ? new(numbers[0], numbers[1], numbers[2]) : LoadReading.Unknown;
    }

    /// <summary>Reads the current load average from the platform.</summary>
    /// <returns>The reading, or <see cref="LoadReading.Unknown"/>.</returns>
    public static LoadReading Current()
    {
        try
        {
            if (File.Exists("/proc/loadavg")) return Parse(File.ReadAllText("/proc/loadavg"));
            var start = new ProcessStartInfo("sysctl", "-n vm.loadavg") { RedirectStandardOutput = true, UseShellExecute = false };
            using var process = Process.Start(start);
            if (process is null) return LoadReading.Unknown;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return Parse(output);
        }
        catch (Exception)
        {
            return LoadReading.Unknown;
        }
    }

    /// <summary>Gets the highest one minute load average a run may start at.</summary>
    /// <param name="cpuCount">The CPU count.</param>
    /// <param name="maxLoad">An explicit limit, or <see langword="null"/> for <see cref="DefaultLoadFactor"/> times the CPU count.</param>
    /// <returns>The limit.</returns>
    public static double Threshold(int cpuCount, double? maxLoad = null) => maxLoad ?? (DefaultLoadFactor * cpuCount);

    /// <summary>Decides whether a measurement may start.</summary>
    /// <param name="reading">The load reading.</param>
    /// <param name="cpuCount">The CPU count.</param>
    /// <param name="maxLoad">An explicit limit, or <see langword="null"/> for the default.</param>
    /// <returns><see langword="true"/> when the machine is quiet enough; an unknown load is refused rather than assumed quiet.</returns>
    public static bool IsQuietEnough(LoadReading reading, int cpuCount, double? maxLoad = null) =>
        !double.IsNaN(reading.OneMinute) && reading.OneMinute <= Threshold(cpuCount, maxLoad);
}
