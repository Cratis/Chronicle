// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SelfReducerAmbiguityAnalyzer.when_analyzing;

public class and_registry_membership_is_not_authority : given.self_reducer_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_error_only_for_definite_declaration_ties() => _result;

    static async Task Verify()
    {
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerAmbiguityAnalyzer>.VerifyAnalyzer(
            Source("""
            public record State
            {
                public static State Reduce(Happened e, State? current) => new();
                public static State Reduce(Happened e, State? current, EventContext context) => new();
            }
            """),
            new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodMustBeUnambiguous, DiagnosticSeverity.Error, "State", "Happened"));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerAmbiguityAnalyzer>.VerifyAnalyzer(
            Source("""
            public record Base { public static State Reduce(Happened e, State? current) => new(); }
            public record State : Base { public new static State Reduce(Happened e, State? current) => new(); }
            """),
            new ExpectedDiagnostic(DiagnosticIds.SelfReducerMethodMustBeUnambiguous, DiagnosticSeverity.Error));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerAmbiguityAnalyzer>.VerifyAnalyzer(Source("""
            public interface IFirst;
            public interface ISecond;
            [EventType] public record PluginEvent : IFirst, ISecond;
            public record State
            {
                public static State Reduce(IFirst e, State? current) => new();
                public static State Reduce(ISecond e, State? current) => new();
            }
            public class Clients
            {
                public void Configure()
                {
                    new Cratis.Chronicle.Registrations.ExplicitArtifacts().RegisterEventType<PluginEvent>();
                    new Cratis.Chronicle.Registrations.ExplicitArtifacts().RegisterReducer<State>();
                }
            }
            """));
        await AnalyzerVerifier<CodeAnalysis.Analyzers.SelfReducerAmbiguityAnalyzer>.VerifyAnalyzer(Source("""
            public record BaseEvent;
            public record DerivedEvent : BaseEvent;
            public record State
            {
                public static State Reduce(BaseEvent e, State? current) => new();
                public static State Reduce(DerivedEvent e, State? current) => new();
            }
            """));
    }
}
