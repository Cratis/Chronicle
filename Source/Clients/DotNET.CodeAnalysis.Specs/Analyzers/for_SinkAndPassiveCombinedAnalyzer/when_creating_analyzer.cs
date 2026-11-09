// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_SinkAndPassiveCombinedAnalyzer;

public class when_creating_analyzer : Specification
{
    CodeAnalysis.Analyzers.SinkAndPassiveCombinedAnalyzer _analyzer;

    void Establish() => _analyzer = new CodeAnalysis.Analyzers.SinkAndPassiveCombinedAnalyzer();

    [Fact] void should_support_chr0069_diagnostic() => _analyzer.SupportedDiagnostics.Any(d => d.Id == DiagnosticIds.SinkCannotBeCombinedWithPassive).ShouldBeTrue();
    [Fact] void should_be_an_error() => _analyzer.SupportedDiagnostics.Single().DefaultSeverity.ShouldEqual(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
}
