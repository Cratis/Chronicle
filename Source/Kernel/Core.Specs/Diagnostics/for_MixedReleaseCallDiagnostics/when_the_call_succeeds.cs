// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MixedReleaseCallDiagnostics;

public class when_the_call_succeeds : given.a_call_filter
{
    void Establish() => _context.Invoke().Returns(Task.CompletedTask);
    Task Because() => Invoke();
    [Fact] void should_not_read_the_cluster_manifest() => _releases.DidNotReceive().Summarize();
    [Fact] void should_not_read_the_interface_method() => _ = _context.DidNotReceive().InterfaceMethod;
    [Fact] void should_not_emit_logs() => _messages.ShouldBeEmpty();
    [Fact] void should_succeed() => _failure.ShouldBeNull();
}
