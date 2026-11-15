// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphViewAnalyzer;

public class when_creating_analyzer : Specification
{
    CodeAnalysis.Analyzers.GraphViewAnalyzer _analyzer;

    void Because() => _analyzer = new();

    [Fact] void should_support_the_root_diagnostic() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.GraphViewRootMustBeNode).ShouldBeTrue();
    [Fact] void should_support_the_edge_diagnostic() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.GraphViewRelationMustFollowEdge).ShouldBeTrue();
    [Fact] void should_support_the_target_diagnostic() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.GraphViewRelationTargetMustBeView).ShouldBeTrue();
    [Fact] void should_support_the_schema_diagnostic() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.GraphViewCannotBeNodeOrEdge).ShouldBeTrue();
}
