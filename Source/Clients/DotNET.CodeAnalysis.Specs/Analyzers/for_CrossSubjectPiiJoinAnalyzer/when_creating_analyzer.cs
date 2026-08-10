// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_CrossSubjectPiiJoinAnalyzer;

public class when_creating_analyzer : Specification
{
    CodeAnalysis.Analyzers.CrossSubjectPiiJoinAnalyzer _analyzer;

    void Establish() => _analyzer = new CodeAnalysis.Analyzers.CrossSubjectPiiJoinAnalyzer();

    [Fact] void should_have_supported_diagnostics() => _analyzer.SupportedDiagnostics.ShouldNotBeEmpty();
    [Fact] void should_support_chr0038_diagnostic() => _analyzer.SupportedDiagnostics.Any(d => d.Id == DiagnosticIds.CrossSubjectPiiJoin).ShouldBeTrue();
    [Fact] void should_keep_chr0038_as_an_error() => _analyzer.SupportedDiagnostics.Single(d => d.Id == DiagnosticIds.CrossSubjectPiiJoin).DefaultSeverity.ShouldEqual(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    [Fact] void should_support_chr0044_as_a_warning() => _analyzer.SupportedDiagnostics.Single(d => d.Id == DiagnosticIds.UnprovableCrossSubjectPiiJoin).DefaultSeverity.ShouldEqual(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
}
