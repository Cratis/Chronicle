// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelBuilderOwnershipAnalyzer.when_analyzing;

public class and_cross_client_declarations_only_warn : given.self_reducer_sources
{
    Task _result;
    void Because() => _result = Verify();
    [Fact] Task should_distinguish_definite_mapping_conflicts_from_possible_peer_ownership() => _result;

    static async Task Verify()
    {
        foreach (var decoration in new[] { "[FromEvent<Happened>]", "" })
        {
            var property = decoration.Length == 0 ? "[FromEvent<Happened>] public string Name { get; init; } = string.Empty;" : "";
            await AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelBuilderOwnershipAnalyzer>.VerifyAnalyzer(Source(
                $"{decoration} public record State {{ {property} public static State Reduce(Happened e, State? current) => new(); }}"),
                new ExpectedDiagnostic(DiagnosticIds.ReadModelBuilderOwnershipMustBeUnambiguous, DiagnosticSeverity.Error));
        }
        await AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelBuilderOwnershipAnalyzer>.VerifyAnalyzer(
            Source("""
            public record State : Cratis.Chronicle.Reactors.IReactor { public static State Reduce(Happened e, State? current) => new(); }
            """),
            new ExpectedDiagnostic(DiagnosticIds.ReadModelBuilderOwnershipMustBeUnambiguous, DiagnosticSeverity.Error));
        foreach (var declaration in new[]
        {
            "public record State([FromEvent<Happened>] string Name) { public static State Reduce(Happened e, State? current) => new(\"\"); }",
            "public record Base { [FromEvent<Happened>] public string Name { get; init; } = string.Empty; } public record State : Base { public static State Reduce(Happened e, State? current) => new(); }",
            "public record State : Cratis.Chronicle.Projections.IProjectionFor<State> { public static State Reduce(Happened e, State? current) => new(); }"
        })
        {
            await AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelBuilderOwnershipAnalyzer>.VerifyAnalyzer(
                Source(declaration),
                new ExpectedDiagnostic(DiagnosticIds.ReadModelBuilderOwnershipMustBeUnambiguous, DiagnosticSeverity.Error));
        }
        foreach (var contract in new[] { "IReducerFor<State>", "Cratis.Chronicle.Projections.IProjectionFor<State>" })
        {
            await AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelBuilderOwnershipAnalyzer>.VerifyAnalyzer(Source(
                $"public record State {{ public static State Reduce(Happened e, State? current) => new(); }} public class Peer : {contract};"),
                new ExpectedDiagnostic(DiagnosticIds.ReadModelBuilderOwnershipMustBeUnambiguous, DiagnosticSeverity.Warning, "Peer"));
        }
        await AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelBuilderOwnershipAnalyzer>.VerifyAnalyzer(Source("""
            [Cratis.Chronicle.ReadModels.Passive]
            [Cratis.Chronicle.EventSequences.EventSequence("custom")]
            [Reducer(isActive: false)]
            public record State { public string Name { get; init; } = string.Empty; public static State Reduce(Happened e, State? current) => new(); }
            public record Classic : IReducerFor<Classic> { public static Classic Reduce(Happened e, Classic? current) => new(); }
            """));
    }
}
