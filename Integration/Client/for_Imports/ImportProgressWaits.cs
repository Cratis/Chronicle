// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Imports;

namespace Cratis.Chronicle.Integration.for_Imports;

public static class ImportProgressWaits
{
    public static async Task<ImportState> WaitTill(this IEventImport import, Func<ImportState, bool> condition, TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        return await import.Progress
            .Select(progress => progress.State)
            .FirstAsync(state => condition(state) || IsTerminal(state))
            .ToTask(deadline.Token);
    }

    public static bool IsTerminal(ImportState state) => state is ImportState.Completed or ImportState.Failed or ImportState.Abandoned;
}
