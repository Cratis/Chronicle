// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_NonAwaitedAssertionAnalyzer;

public class when_scenario_awaitable_shapes_differ : given.scenario_terminal_calls
{
    [Theory]
    [InlineData("Task")]
    [InlineData("Task<int>")]
    [InlineData("ValueTask")]
    [InlineData("ValueTask<int>")]
    public async Task should_warn_only_when_framework_awaitables_are_discarded(string returnType)
    {
        foreach (var call in Calls)
        {
            await AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
                CreateSource("void Run() { {|#0:" + call + "|}; }", returnType, returnType),
                new ExpectedDiagnostic("CHR0082", DiagnosticSeverity.Warning));
            await AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
                CreateSource("async Task Run() { await " + call + "; }", returnType, returnType));
        }
    }

    [Theory]
    [InlineData("void")]
    [InlineData("CustomAwaitable")]
    [InlineData("Contoso.Task")]
    [InlineData("Contoso.ValueTask")]
    public Task should_ignore_non_framework_return_types(string returnType) =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.NonAwaitedAssertionAnalyzer>.VerifyAnalyzer(
            CreateSource("void Run() { given.Events(); }", returnType) + """

            public class CustomAwaitable
            {
                public System.Runtime.CompilerServices.TaskAwaiter GetAwaiter() => Task.CompletedTask.GetAwaiter();
            }
            namespace Contoso
            {
                public class Task : CustomAwaitable { }
                public class ValueTask : CustomAwaitable { }
            }
            """);
}
