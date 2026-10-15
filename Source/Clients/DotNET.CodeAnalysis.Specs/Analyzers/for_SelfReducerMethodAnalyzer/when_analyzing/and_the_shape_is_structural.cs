// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerMethodAnalyzer.when_analyzing;

public class and_the_shape_is_structural : given.self_reducer_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_warn_on_near_misses_without_requiring_event_membership() => _result;

    static async Task Verify()
    {
        string[] invalid =
        [
            "private static State Reduce(Happened e, State? current) => new();",
            "public static State Reduce<T>(Happened e, State? current) => new();",
            "public static State Reduce(ref Happened e, State? current) => new();",
            "public static State Reduce(Happened e, State? current = null) => new();",
            "public static State Reduce(Happened e, State? current, params object[] services) => new();",
            "public static State Reduce(Happened e, State current) => current;",
            "public static State Reduce(object e, State? current) => new();",
            "public static State Reduce(Happened e, State? current, string service) => new();",
            "public static void Reduce(Happened e, State? current) { }",
            "public static Task Reduce(Happened e, State? current) => Task.CompletedTask;",
            "public static ValueTask<State> Reduce(Happened e, State? current) => new(new State());",
            "public static Task<object> Reduce(Happened e, State? current) => Task.FromResult<object>(new State());",
            "public static State Reduce(Happened e) => new();"
        ];
        foreach (var method in invalid)
        {
            await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerMethodAnalyzer>.VerifyAnalyzer(Source("public record State { " + method + " }"),
                new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodSignatureMustMatchAllowed, DiagnosticSeverity.Warning, "Reduce"));
        }
        foreach (var result in new[] { "State", "State?", "Task<State>", "Task<State?>" })
        {
            var body = result.StartsWith("Task", StringComparison.Ordinal) ? $"Task.FromResult<{(result.Contains('?') ? "State?" : "State")}>(new State())" : "new State()";
            await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerMethodAnalyzer>.VerifyAnalyzer(Source($"public record State {{ public static {result} Reduce(Happened e, State? current, EventContext context) => {body}; }}"));
        }
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerMethodAnalyzer>.VerifyAnalyzer(Source("""
            #nullable disable
            public record State { public static State Reduce(Happened e, State current) => new(); }
            """));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerMethodAnalyzer>.VerifyAnalyzer(Source("""
            public record State : IReducerFor<State> { public static State Reduce(Happened e, State current) => new(); }
            public record Helper { public static int Reduce(Happened e) => 1; }
            public abstract record Abstract { public static Abstract Reduce(Happened e, Abstract? current) => current!; }
            public record Generic<T> { public static Generic<T> Reduce(Happened e, Generic<T>? current) => new(); }
            public record Base { public static Derived Reduce(Happened e, Derived? current) => new(); }
            public record Derived : Base;
            """));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerMethodAnalyzer>.VerifyAnalyzer(
            Source("""
            public partial record State { public static State Reduce(Happened e, State? current) => new(); }
            public partial record State { public static void Reduce(string e, State? current) { } }
            """),
            new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodSignatureMustMatchAllowed, DiagnosticSeverity.Warning));
    }
}
