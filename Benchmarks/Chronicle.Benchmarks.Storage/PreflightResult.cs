// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>The outcome of the checks made before anything is started.</summary>
/// <param name="ExitCode">0 to go ahead, 2 for a missing confirmation, <see cref="MachineLoad.RefusedExitCode"/> when the machine is too loaded.</param>
/// <param name="Message">What to tell the user.</param>
public sealed record PreflightResult(int ExitCode, string Message)
{
    /// <summary>Gets whether the run may start.</summary>
    public bool CanStart => ExitCode == 0;

    /// <summary>Checks whether a run may start.</summary>
    /// <param name="options">The options.</param>
    /// <param name="load">The current load.</param>
    /// <param name="cpuCount">The CPU count.</param>
    /// <returns>The result.</returns>
    public static PreflightResult Check(StorageRunOptions options, LoadReading load, int cpuCount)
    {
        if (options.Profile.RequiresConfirmation && !options.Confirmed)
        {
            return new(2, $"The {options.Profile.Name} profile is long (up to {options.Profile.TimeoutMinutes} minutes) and needs several GB; pass {StorageRunOptions.ConfirmFlag} to start it.");
        }

        if (!MachineLoad.IsQuietEnough(load, cpuCount, options.MaxLoad))
        {
            return new(MachineLoad.RefusedExitCode, $"Machine too loaded to measure: 1-minute load {load.OneMinute:0.00}, limit {MachineLoad.Threshold(cpuCount, options.MaxLoad):0.00}. No number is reported from a loaded machine.");
        }

        return new(0, "ok");
    }
}
