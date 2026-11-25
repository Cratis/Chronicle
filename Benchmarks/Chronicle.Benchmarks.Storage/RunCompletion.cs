// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Publishes the final artifacts before tearing down provider hosts and choosing the exit code.</summary>
/// <param name="Ledgers">The ledgers of providers that started.</param>
/// <param name="ResultDirectory">The run's artifact directory.</param>
/// <param name="CutShort">Whether the profile budget was exhausted.</param>
/// <param name="ExitCode">The measured verdict's exit code.</param>
internal sealed record RunCompletion(IReadOnlyList<SettingsLedger> Ledgers, string ResultDirectory, bool CutShort, int ExitCode)
{
    /// <summary>Writes the final artifacts and awaits the bounded provider teardown.</summary>
    /// <param name="teardown">The provider teardown operation.</param>
    /// <returns>Exit code 4 when the budget was exhausted, otherwise the measured verdict.</returns>
    public async Task<int> Finish(Func<Task> teardown)
    {
        var loadAtEnd = MachineLoad.Current();
        foreach (var ledger in Ledgers)
        {
            await File.WriteAllTextAsync(Path.Combine(ResultDirectory, ledger.Provider, "ledger.json"), ledger.WithLoadAtEnd(loadAtEnd).ToJson());
        }

        await File.WriteAllTextAsync(Path.Combine(ResultDirectory, "summary.md"), RunSummary.Combine(Ledgers, ResultDirectory, CutShort));
        await teardown();
        return CutShort ? 4 : ExitCode;
    }
}
