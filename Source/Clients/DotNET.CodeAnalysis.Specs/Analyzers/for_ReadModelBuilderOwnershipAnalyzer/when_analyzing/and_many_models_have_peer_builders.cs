// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelBuilderOwnershipAnalyzer.when_analyzing;

public class and_many_models_have_peer_builders : given.self_reducer_sources
{
    const int ModelCount = 128;
    Task _result;

    void Because()
    {
        var declarations = Enumerable.Range(0, ModelCount).Select(index => $$"""
            public record State{{index}} { public static State{{index}} Reduce(Happened e, State{{index}}? current) => new(); }
            public class Peer{{index}} : IReducerFor<State{{index}}>, Cratis.Chronicle.Projections.IProjectionFor<State{{index}}>;
            public class Unrelated{{index}};
            """);
        var expected = Enumerable.Range(0, ModelCount).Select(index =>
            new ExpectedDiagnostic(DiagnosticIds.ReadModelBuilderOwnershipMustBeUnambiguous, DiagnosticSeverity.Warning, $"State{index}", $"Peer{index}"));
        _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelBuilderOwnershipAnalyzer>.VerifyAnalyzer(
            Source(string.Join(Environment.NewLine, declarations)), expected.ToArray());
    }

    [Fact] Task should_report_each_peer_once_without_cross_model_ownership() => _result;
}
