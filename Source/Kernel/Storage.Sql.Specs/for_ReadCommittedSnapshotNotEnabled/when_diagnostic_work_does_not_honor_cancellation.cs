// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_ReadCommittedSnapshotNotEnabled;

public class when_diagnostic_work_does_not_honor_cancellation : Specification
{
    ReadCommittedSnapshotDiagnostics _diagnostics;
    readonly TaskCompletionSource<ReadCommittedSnapshotDiagnostics> _late = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Because()
    {
        try
        {
            _diagnostics = await SqlServerReadCommittedSnapshot.SnapshotWithin(
                async _ =>
                {
                    try
                    {
                        return await _late.Task;
                    }
                    finally
                    {
                        _finished.TrySetResult();
                    }
                },
                TimeSpan.FromMilliseconds(100));
        }
        finally
        {
            // A non-cancelable identity provider can fault after the deadline. The helper must observe that fault.
            _late.TrySetException(new InvalidOperationException("synthetic private diagnostic failure"));
            await _finished.Task;
        }
    }

    [Fact] void should_return_a_timeout_without_waiting_for_identity_preparation() => _diagnostics.Unavailable.ShouldEqual("timeout");
    [Fact] void should_not_report_the_late_private_failure() => _diagnostics.Describe().ShouldNotContain("synthetic private");
}
