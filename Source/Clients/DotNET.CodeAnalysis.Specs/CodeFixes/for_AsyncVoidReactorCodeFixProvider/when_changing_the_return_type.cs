// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.CodeFixes.for_AsyncVoidReactorCodeFixProvider;

public class when_changing_the_return_type : Analyzers.for_ReactorMethodAnalyzer.given.a_reactor_method_analyzer
{
    [Theory]
    [InlineData("public async void On(Happened @event) { await global::System.Threading.Tasks.Task.Yield(); }")]
    [InlineData("public async void On(Happened @event) => await global::System.Threading.Tasks.Task.Delay(1);")]
    [InlineData("[Cratis.Chronicle.Reactors.OnceOnly] public async void On(Happened @event, IService service) { await global::System.Threading.Tasks.Task.Yield(); }")]
    [InlineData("public async /* before */ void /* after */ On(Happened @event) { /* body */ await global::System.Threading.Tasks.Task.Yield(); }")]
    public async Task should_preserve_the_handler_and_produce_compiling_task_code(string method)
    {
        var source = CreateSource($$"""
            [EventType] public class Happened { }
            public interface IService { }
            public class Task { }
            public class Reactor : Cratis.Chronicle.Reactors.IReactor
            {
                {{method}}
            }
            """);
        var fixedSource = source.Replace("void ", "global::System.Threading.Tasks.Task ", StringComparison.Ordinal);
        await CodeFixVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer, CodeAnalysis.CodeFixes.AsyncVoidReactorCodeFixProvider>.VerifyCodeFix(
            source, fixedSource, new ExpectedDiagnostic(DiagnosticIds.ReactorMethodMustNotBeAsyncVoid, DiagnosticSeverity.Warning));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(fixedSource);
    }

    [Fact]
    public void should_offer_fix_all() =>
        new CodeAnalysis.CodeFixes.AsyncVoidReactorCodeFixProvider().GetFixAllProvider().ShouldNotBeNull();
}
