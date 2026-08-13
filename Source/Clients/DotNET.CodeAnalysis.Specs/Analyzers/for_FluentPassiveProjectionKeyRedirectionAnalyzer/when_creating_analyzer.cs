// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentPassiveProjectionKeyRedirectionAnalyzer;

public class when_creating_analyzer : Specification
{
    CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer _analyzer;

    void Establish() => _analyzer = new CodeAnalysis.Analyzers.FluentPassiveProjectionKeyRedirectionAnalyzer();

    [Fact] void should_have_supported_diagnostics() => _analyzer.SupportedDiagnostics.ShouldNotBeEmpty();
    [Fact] void should_support_chr0046_diagnostic() => _analyzer.SupportedDiagnostics.Any(d => d.Id == DiagnosticIds.PassiveProjectionKeyRedirection).ShouldBeTrue();
    [Fact] void should_keep_chr0046_as_a_warning() => _analyzer.SupportedDiagnostics.Single(d => d.Id == DiagnosticIds.PassiveProjectionKeyRedirection).DefaultSeverity.ShouldEqual(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
}
