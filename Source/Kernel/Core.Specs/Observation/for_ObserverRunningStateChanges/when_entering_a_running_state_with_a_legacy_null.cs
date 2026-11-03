// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverRunningStateChanges;

public class when_entering_a_running_state_with_a_legacy_null : Specification
{
    (DateTimeOffset? ChangedAt, ObserverRunningState Settled, bool Changed) _result;

    void Because() => _result = ObserverRunningStateChanges.Apply(null, ObserverRunningState.Active, ObserverRunningState.Active, DateTimeOffset.UnixEpoch);

    [Fact] void should_keep_the_timestamp_unknown() => _result.ChangedAt.ShouldBeNull();
    [Fact] void should_not_report_a_change() => _result.Changed.ShouldBeFalse();
}
