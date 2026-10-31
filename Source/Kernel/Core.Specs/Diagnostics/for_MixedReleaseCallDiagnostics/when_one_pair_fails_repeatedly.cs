// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MixedReleaseCallDiagnostics;

public class when_one_pair_fails_repeatedly : given.a_call_filter
{
    async Task Because()
    {
        await Invoke();
        await Invoke();
    }
    [Fact] void should_report_the_pair_only_once() => _messages.Count.ShouldEqual(1);
    [Fact] void should_read_the_release_summary_only_once() => _releases.Received(1).Summarize();
}
