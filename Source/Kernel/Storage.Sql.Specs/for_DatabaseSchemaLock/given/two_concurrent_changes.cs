// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.given;

/// <summary>
/// Runs two schema changes on different sessions, the second starting while the first holds the lock.
/// </summary>
/// <param name="database">The database both change.</param>
public class two_concurrent_changes(a_database database)
{
    public bool HeldElsewhereDuringFirst { get; private set; }
    public bool SecondStartedWhileFirstHeld { get; private set; }
    public bool SecondStarted { get; private set; }

    public async Task Run()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = Task.Run(() => DatabaseSchemaLock.Run(database.NewContext(), async () =>
        {
            firstStarted.SetResult();
            await releaseFirst.Task;
        }));

        // A first change that fails before it starts surfaces its own error rather than a timeout.
        await Task.WhenAny(firstStarted.Task, first).WaitAsync(TimeSpan.FromSeconds(30));
        if (first.IsCompleted)
        {
            await first;
        }

        HeldElsewhereDuringFirst = await database.IsHeldElsewhere();

        var second = Task.Run(() => DatabaseSchemaLock.Run(database.NewContext(), () =>
        {
            SecondStarted = true;
            return Task.CompletedTask;
        }));

        // Long enough for an unguarded change to have started.
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        SecondStartedWhileFirstHeld = SecondStarted;

        releaseFirst.SetResult();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(30));
    }
}
