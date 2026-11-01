// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Serialization;

namespace Cratis.Chronicle.Diagnostics.for_MixedReleaseCallDiagnostics.when_a_grain_call_fails;

public class and_the_wire_failure_is_wrapped : given.a_call_filter
{
    void Establish() => _original = new OrleansException("wrapper", new AggregateException(new InvalidOperationException("unrelated"), new UnsupportedWireTypeException("wire-name")));
    Task Because() => Invoke();
    [Fact] void should_find_the_wire_failure_in_the_chain() => _messages.Single().ShouldContain("UnsupportedWireTypeException");
    [Fact] void should_rethrow_the_original_wrapper() => _failure.ShouldEqual(_original);
    [Fact] void should_not_log_the_wrapper_text() => _messages.Single().ShouldNotContain("wrapper");
}
