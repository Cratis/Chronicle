// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// Legacy System databases the kernel was not allowed to drop are stale copies it never reads, so they are reported
/// on every start without stopping it.
/// </summary>
public class and_legacy_system_data_was_left_behind : given.a_startup_task
{
    Exception _error;

    void Establish() => _storage.FindRetainedLegacySystemEventStoreData().Returns("databases remain under 'System'");

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_start() => _error.ShouldBeNull();
    [Fact] void should_report_what_remains() => LogMessages.Any(_ => _.Contains("databases remain under 'System'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] async Task should_apply_the_patches() => await _patchRunner.Received(1).ApplyPatches(Arg.Any<CancellationToken>());
}
