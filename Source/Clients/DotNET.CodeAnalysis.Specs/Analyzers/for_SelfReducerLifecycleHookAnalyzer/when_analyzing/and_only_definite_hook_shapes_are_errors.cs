// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerLifecycleHookAnalyzer.when_analyzing;

public class and_only_definite_hook_shapes_are_errors : given.self_reducer_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_reject_invalid_hooks_and_allow_independent_optional_hooks() => _result;

    static async Task Verify()
    {
        string[] invalid =
        [
            "public void BeginReplay() { }",
            "private static Task BeginReplay() => Task.CompletedTask;",
            "public static void BeginReplay() { }",
            "public static Task<State> BeginReplay() => Task.FromResult(new State());",
            "public static ValueTask BeginReplay() => ValueTask.CompletedTask;",
            "public static Task BeginReplay<T>() => Task.CompletedTask;",
            "public static Task BeginReplay(ref Service service) => Task.CompletedTask;",
            "public static Task BeginReplay(Service? service = null) => Task.CompletedTask;",
            "public static Task BeginReplay(params Service[] services) => Task.CompletedTask;",
            "public static Task BeginReplay(State current) => Task.CompletedTask;",
            "public static Task BeginReplay(EventContext context) => Task.CompletedTask;",
            "public static Task BeginReplay(Partition partition) => Task.CompletedTask;",
            "public static Task BeginReplay(int service) => Task.CompletedTask;",
            "public static Task BeginReplayPartition() => Task.CompletedTask;",
            "public static Task EndReplayPartition(Service service, Partition partition) => Task.CompletedTask;",
            "public static Task BeginReplay(Marked e) => Task.CompletedTask;"
        ];
        foreach (var method in invalid)
        {
            await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerLifecycleHookAnalyzer>.VerifyAnalyzer(Source(
                "public class Service; [EventType] public record Marked; public record State { public static State Reduce(Happened e, State? current) => new(); " + method + " }"),
                new ExpectedDiagnostic(DiagnosticIds.SelfReducerLifecycleHookSignatureMustMatchAllowed, DiagnosticSeverity.Error));
        }
        foreach (var name in new[] { "BeginReplay", "EndReplay", "BeginReplayPartition", "EndReplayPartition" })
        {
            var parameters = name.EndsWith("Partition", StringComparison.Ordinal) ? "Partition partition, Service service" : "Service service";
            await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerLifecycleHookAnalyzer>.VerifyAnalyzer(Source(
                $"public class Service; public record State {{ public static State Reduce(Happened e, State? current) => new(); public static Task {name}({parameters}) => Task.CompletedTask; }}"));
        }
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerLifecycleHookAnalyzer>.VerifyAnalyzer(Source("""
            public record Base { public static Task BeginReplay() => Task.CompletedTask; }
            public record State : Base
            {
                public static State Reduce(Happened e, State? current) => new();
                public new static Task BeginReplay() => Task.CompletedTask;
            }
            """),
            new ExpectedDiagnostic(DiagnosticIds.SelfReducerLifecycleHookSignatureMustMatchAllowed, DiagnosticSeverity.Error),
            new ExpectedDiagnostic(DiagnosticIds.SelfReducerLifecycleHookSignatureMustMatchAllowed, DiagnosticSeverity.Error));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerLifecycleHookAnalyzer>.VerifyAnalyzer(Source("public record Helper { public static void BeginReplay() { } }"));
    }
}
