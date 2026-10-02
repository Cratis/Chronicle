// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

public class and_a_legacy_half_swap_has_no_completion_marker_on_sqlite : given.a_legacy_half_swapped_replay<SqlSinkHarness>
{
    protected override SqlSinkHarness CreateHarnessWithInterceptor(IInterceptor interceptor) => new() { Interceptors = [interceptor] };

    Task Because() => RetryPromotion();

    [Fact] void should_have_reproduced_the_legacy_partial_commit() => _legacyAttemptInterrupted.ShouldBeTrue();
    [Fact] void should_report_the_ambiguous_promotion() => _error.ShouldBeOfExactType<UnverifiedReplayBackup>();
    [Fact] void should_leave_the_recreated_primary_untouched() => _primary.ShouldBeNull();
    [Fact] void should_preserve_the_rebuilt_rows() => _replay.ShouldEqual(2);
    [Fact] void should_preserve_the_original_backup() => _backup.ShouldEqual(1);
}
