// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayRestartBackoff;

public class when_reset_after_restarts : Specification
{
    static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    ReplayRestartBackoff _backoff;

    void Establish()
    {
        _backoff = new();
        _backoff.Record(_now);
        _backoff.Record(_now);
        _backoff.Record(_now);
    }

    void Because() => _backoff.Reset();

    [Fact] void should_allow_a_restart() => _backoff.IsDue(_now).ShouldBeTrue();
    [Fact] void should_forget_the_attempts() => _backoff.Attempts.ShouldEqual(0);
}
