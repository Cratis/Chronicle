// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_ReadCommittedSnapshotNotEnabled;

public class when_diagnostic_work_is_canceled : Specification
{
    Exception _failure;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var capturing = SqlServerReadCommittedSnapshot.SnapshotWithin(
            async token =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return new ReadCommittedSnapshotDiagnostics(null, null, null, null, null, []);
            },
            TimeSpan.FromSeconds(5),
            cancellation.Token);
        await started.Task;
        await cancellation.CancelAsync();
        _failure = await Catch.Exception(async () => await capturing);
    }

    [Fact] void should_propagate_caller_cancellation() => (_failure is OperationCanceledException).ShouldBeTrue();
}
