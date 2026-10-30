// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MixedReleaseCallDiagnostics;

public class when_many_calls_fail : given.a_call_filter
{
    async Task Because()
    {
        var methods = typeof(Enumerable).GetMethods().DistinctBy(_ => _.Name).Take(34).ToArray();
        foreach (var method in methods)
        {
            _context.InterfaceMethod.Returns(method);
            await Invoke();
        }
        foreach (var method in methods)
        {
            _context.InterfaceMethod.Returns(method);
            await Invoke();
        }
    }
    [Fact] void should_bound_distinct_pair_reports() => _messages.Count(_ => _.Contains("Mixed-release grain call failed", StringComparison.Ordinal)).ShouldEqual(32);
    [Fact] void should_emit_one_suppression_message() => _messages.Count(_ => _.Contains("further failures suppressed", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_keep_rethrowing_the_original_failure() => _failure.ShouldEqual(_original);
}
