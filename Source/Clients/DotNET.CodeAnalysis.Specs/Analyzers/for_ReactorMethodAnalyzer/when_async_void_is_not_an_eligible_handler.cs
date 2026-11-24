// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMethodAnalyzer;

public class when_async_void_is_not_an_eligible_handler : given.a_reactor_method_analyzer
{
    [Theory]
    [InlineData("public void On(Happened @event) { }")]
    [InlineData("public async Task On(Happened @event) { await Task.Yield(); }")]
    [InlineData("public Task On(Happened @event) => Task.CompletedTask;")]
    [InlineData("public Task<Happened> On(Happened @event) => Task.FromResult(@event);")]
    [InlineData("private async void Helper(Unknown value) { await Task.Yield(); }")]
    [InlineData("public static async void On(Happened @event) { await Task.Yield(); }")]
    [InlineData("public async void Helper() { await Task.Yield(); }")]
    [InlineData("public async void Helper(Unknown value, int count) { await Task.Yield(); }")]
    [InlineData("public async void Helper(object value) { await Task.Yield(); }")]
    [InlineData("public async void Helper(string value) { await Task.Yield(); }")]
    [InlineData("public async void Helper<T>(T value) { await Task.Yield(); }")]
    public Task should_not_warn(string method) =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource($$"""
            [EventType] public class Happened { }
            public class Unknown { }
            public class Reactor : Cratis.Chronicle.Reactors.IReactor
            {
                {{method}}
            }
            """));

    [Fact]
    public Task should_ignore_unrelated_classes() =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource("""
            [EventType] public class Happened { }
            public class NotAReactor
            {
                public async void On(Happened @event) { await Task.Yield(); }
            }
            """));

    [Fact]
    public Task should_preserve_the_missing_event_attribute_diagnostic() =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource("""
            public class Unknown { }
            public class Reactor : Cratis.Chronicle.Reactors.IReactor
            {
                [Cratis.Chronicle.Reactors.OnceOnly]
                public async void On({|#0:Unknown @event|}) { await Task.Yield(); }
            }
            """), new ExpectedDiagnostic(DiagnosticIds.ReactorEventParameterMustHaveAttribute, DiagnosticSeverity.Error));

    [Fact]
    public Task should_preserve_the_invalid_dependency_diagnostic() =>
        AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource("""
            [EventType] public class Happened { }
            public class Reactor : Cratis.Chronicle.Reactors.IReactor
            {
                public async void {|#0:On|}(Happened @event, int count) { await Task.Yield(); }
            }
            """), new ExpectedDiagnostic(DiagnosticIds.ReactorMethodSignatureMustMatchAllowed, DiagnosticSeverity.Warning));
}
