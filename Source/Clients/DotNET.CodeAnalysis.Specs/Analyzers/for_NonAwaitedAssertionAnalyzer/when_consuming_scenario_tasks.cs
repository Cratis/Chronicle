// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer;

public class when_consuming_scenario_tasks : given.scenario_terminal_calls
{
    [Theory]
    [InlineData("async Task Run() { await CALL; }")]
    [InlineData("Task Run() { return CALL; }")]
    [InlineData("Task Run() => CALL;")]
    [InlineData("Task Run() => ((CALL));")]
    [InlineData("Task Run() { Task Local() => CALL; return Local(); }")]
    [InlineData("void Run() { var task = CALL; }")]
    [InlineData("void Run() { captured = CALL; }")]
    [InlineData("void Run() { Task _; _ = CALL; }")]
    [InlineData("void Run() { Task _; _ = ((CALL)); }")]
    [InlineData("void Run(Task _) { _ = CALL; }")]
    [InlineData("Task Run() => _ = CALL;")]
    [InlineData("Task Run() { return _ = CALL; }")]
    [InlineData("Task Run() => Task.WhenAll(_ = CALL);")]
    [InlineData("Task Run() => Task.WhenAll(CALL);")]
    [InlineData("async Task Run() { await ((CALL)); }")]
    public async Task should_not_warn_for_each_allowlisted_method(string member)
    {
        foreach (var call in Calls)
        {
            await AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
                CreateSource(member.Replace("CALL", call, StringComparison.Ordinal)));
        }
    }

    [Fact]
    public Task should_not_treat_an_underscore_field_as_a_discard() =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
            CreateSource("Task _; void Run() { _ = given.Events(); }"));
}
