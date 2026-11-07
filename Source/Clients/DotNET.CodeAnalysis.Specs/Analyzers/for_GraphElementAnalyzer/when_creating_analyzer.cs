// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_GraphElementAnalyzer;

public class when_creating_analyzer : Specification
{
    CodeAnalysis.Analyzers.GraphElementAnalyzer _analyzer;

    void Because() => _analyzer = new();

    [Fact] void should_support_the_nested_object_diagnostic() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.GraphElementPropertyMustBePrimitive).ShouldBeTrue();
    [Fact] void should_support_the_key_diagnostic() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.GraphElementKeyMustBeDeclaredOnce).ShouldBeTrue();
    [Fact] void should_support_the_read_model_diagnostic() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.GraphElementCannotBeReadModel).ShouldBeTrue();
}
