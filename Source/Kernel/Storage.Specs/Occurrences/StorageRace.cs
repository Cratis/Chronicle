// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// Starts two independent provider calls together without sleeps or polling.
/// </summary>
public static class StorageRace
{
    /// <summary>
    /// Releases both calls only after both workers reach the starting line.
    /// </summary>
    /// <param name="first">The first call.</param>
    /// <param name="second">The second call.</param>
    /// <typeparam name="T">The storage result type.</typeparam>
    /// <returns>The two observed outcomes.</returns>
    public static Task<T[]> Run<T>(Func<Task<T>> first, Func<Task<T>> second)
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrivals = 0;

        async Task<T> Start(Func<Task<T>> action)
        {
            if (Interlocked.Increment(ref arrivals) == 2)
            {
                ready.SetResult();
            }

            await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
            return await action();
        }

        return Task.WhenAll(Task.Run(() => Start(first)), Task.Run(() => Start(second)));
    }
}
