// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentKeyRedirectionPiiAnalyzer;

public class when_creating_analyzer : Specification
{
    CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer _analyzer;

    void Establish() => _analyzer = new CodeAnalysis.Analyzers.FluentKeyRedirectionPiiAnalyzer();

    [Fact] void should_have_supported_diagnostics() => _analyzer.SupportedDiagnostics.ShouldNotBeEmpty();
    [Fact] void should_support_chr0043_diagnostic() => _analyzer.SupportedDiagnostics.Any(d => d.Id == DiagnosticIds.KeyRedirectionPii).ShouldBeTrue();
    [Fact] void should_keep_chr0043_as_a_warning() => _analyzer.SupportedDiagnostics.Single(d => d.Id == DiagnosticIds.KeyRedirectionPii).DefaultSeverity.ShouldEqual(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
}
