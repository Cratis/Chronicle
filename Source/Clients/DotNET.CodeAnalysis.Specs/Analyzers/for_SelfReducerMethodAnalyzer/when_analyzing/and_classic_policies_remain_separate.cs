// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerMethodAnalyzer.when_analyzing;

public class and_classic_policies_remain_separate : given.self_reducer_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_keep_model_state_and_registry_evidence_out_of_classic_errors() => _result;

    static async Task Verify()
    {
        var source = Source("""
            public record State
            {
                public string Name { get; set; } = string.Empty;
                public static State Reduce(Happened e, State? current) => new();
            }
            public class Clients
            {
                public void Configure()
                {
                    new Cratis.Chronicle.Registrations.ExplicitArtifacts().RegisterReducer<State>();
                    new Cratis.Chronicle.Registrations.ExplicitArtifacts().RegisterEventType(typeof(Happened));
                }
            }
            """);
        await AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMethodAnalyzer>.VerifyAnalyzer(source);
        await AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMustNotHaveMutableStateAnalyzer>.VerifyAnalyzer(source);
        await AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerCurrentReadModelMustBeNullableAnalyzer>.VerifyAnalyzer(source);
        await AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerCurrentReadModelMustBeNullableAnalyzer>.VerifyAnalyzer(
            Source("public record State { public static State Reduce(Happened e, State current) => current; }"),
            new ExpectedDiagnostic(DiagnosticIds.ReducerCurrentReadModelMustBeNullable, DiagnosticSeverity.Warning));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerCurrentReadModelMustBeNullableAnalyzer>.VerifyAnalyzer(Source("""
            #nullable disable
            public record State { public static State Reduce(Happened e, State current) => current; }
            """));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerEventStoreAndEventSequenceAnalyzer>.VerifyAnalyzer(
            Source("""
            [EventStore("foreign")]
            [Cratis.Chronicle.EventSequences.EventLog]
            public record Base;
            public record State : Base { public static State Reduce(Happened e, State? current) => new(); }
            """),
            new ExpectedDiagnostic(DiagnosticIds.ReducerCannotCombineEventStoreWithExplicitEventSequence, DiagnosticSeverity.Error));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMultipleEventStoresAnalyzer>.VerifyAnalyzer(
            Source("""
            public interface IImported;
            [EventType, EventStore("first")] public record First : IImported;
            [EventStore("second")] public record Second : IImported;
            public record State { public static State Reduce(IImported e, State? current) => new(); }
            public class Clients
            {
                public void Configure() => new Cratis.Chronicle.Registrations.ExplicitArtifacts().RegisterEventType<Second>();
            }
            """),
            new ExpectedDiagnostic(DiagnosticIds.ReducerEventTypesMustBeFromSameEventStore, DiagnosticSeverity.Warning));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMultipleEventStoresAnalyzer>.VerifyAnalyzer(Source("""
            [EventType, EventStore("first")] public record First;
            [EventType, EventStore("second")] public record Second;
            [EventStore("override")]
            public record State
            {
                public static State Reduce(First e, State? current) => new();
                public static State Reduce(Second e, State? current) => new();
            }
            """));
    }
}
