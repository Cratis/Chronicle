// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_changing_running_state;

public class and_an_older_timestamp_save_succeeds : given.an_observer_with_a_superseded_timestamp_write
{
    Task Because() => SupersedeTimestampWrite(true);

    [Fact] void should_preserve_the_reloaded_quarantine_time() => _persistedRunningStateChangedAt.ShouldEqual(_newerTimestamp);
    [Fact] void should_persist_the_newer_debt_once() => _savedTimestamps.ShouldEqual(new DateTimeOffset?[] { _now, _now });
    [Fact] void should_not_fail_the_original_write() => _writeError.ShouldBeNull();
}
