// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ModelBoundProjectionAttributeAnalyzer;

public class when_creating_analyzer : Specification
{
    CodeAnalysis.Analyzers.ModelBoundProjectionAttributeAnalyzer _analyzer;

    void Establish() => _analyzer = new CodeAnalysis.Analyzers.ModelBoundProjectionAttributeAnalyzer();

    [Fact] void should_have_supported_diagnostics() => _analyzer.SupportedDiagnostics.ShouldNotBeEmpty();
    [Fact] void should_support_chr0003_diagnostic() => _analyzer.SupportedDiagnostics.Any(d => d.Id == DiagnosticIds.ModelBoundProjectionEventTypeMustHaveAttribute).ShouldBeTrue();
}
