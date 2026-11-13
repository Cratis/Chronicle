// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog;

public class when_many_new_kinds_fail_at_once : Specification
{
    const int MaxKinds = 16;

    RepeatedFailureLog _log;
    int _written;

    void Establish() => _log = new(TimeSpan.FromSeconds(10), 1, new ManualTime(), MaxKinds, untrackedAllowedPerWindow: 0);

    void Because() => Parallel.For(0, 10_000, i =>
    {
        if (_log.ShouldWrite("Category", $"Failure {i}", LogLevel.Error, new TimeoutException()))
        {
            Interlocked.Increment(ref _written);
        }
    });

    [Fact] void should_never_track_more_kinds_than_the_bound() => (_written <= MaxKinds).ShouldBeTrue();
}
