// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MixedReleaseCallDiagnostics.when_a_grain_call_fails;

public class and_the_failure_is_not_a_release_mismatch : given.a_call_filter
{
    void Establish() => _original = new InvalidOperationException("work failed");
    Task Because() => Invoke();
    [Fact] void should_not_emit_release_diagnostics() => _messages.ShouldBeEmpty();
    [Fact] void should_not_read_the_cluster_manifest() => _releases.DidNotReceive().Summarize();
    [Fact] void should_preserve_the_original_failure() => _failure.ShouldEqual(_original);
}
