// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer;

public class when_discarding_scenario_tasks : given.scenario_terminal_calls
{
    [Theory]
    [InlineData("void Run() { CALL; }")]
    [InlineData("async Task Run() { CALL; await Task.Yield(); }")]
    [InlineData("void Run() { _ = CALL; }")]
    [InlineData("async Task Run() { _ = CALL; await Task.Yield(); }")]
    [InlineData("void Run() { _ = ((CALL)); }")]
    [InlineData("void Run() => CALL;")]
    [InlineData("void Run() => _ = ((CALL));")]
    [InlineData("void Run() { void Local() => CALL; Local(); }")]
    public async Task should_warn_for_each_allowlisted_method(string member)
    {
        foreach (var call in Calls)
        {
            await AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
                CreateSource(member.Replace("CALL", "{|#0:" + call + "|}", StringComparison.Ordinal)),
                new ExpectedDiagnostic("CHR0082", DiagnosticSeverity.Warning));
        }
    }
}
