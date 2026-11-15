// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodPatternAnalyzer;

public class when_creating_analyzer : Specification
{
    CodeAnalysis.Analyzers.BatchMethodPatternAnalyzer _analyzer;

    void Establish() => _analyzer = new();

    [Fact] void should_support_batch_method_matches_unobserved_event_type() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.BatchMethodMatchesUnobservedEventType).ShouldBeTrue();
}
