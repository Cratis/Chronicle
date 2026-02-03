// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Analyzers;

namespace Cratis.Chronicle.CodeAnalysis.Specs.for_EventSequenceAppendAnalyzer;

public class when_creating_analyzer : Specification
{
    EventSequenceAppendAnalyzer _analyzer;

    void Establish() => _analyzer = new EventSequenceAppendAnalyzer();

    [Fact] void should_have_supported_diagnostics() => _analyzer.SupportedDiagnostics.ShouldNotBeEmpty();
    [Fact] void should_support_chr0001_diagnostic() => _analyzer.SupportedDiagnostics.Any(d => d.Id == DiagnosticIds.EventTypeMustHaveAttributeWhenAppended).ShouldBeTrue();
}
