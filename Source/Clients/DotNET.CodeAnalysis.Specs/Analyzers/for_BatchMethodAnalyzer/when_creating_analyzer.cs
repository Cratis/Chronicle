// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodAnalyzer;

public class when_creating_analyzer : Specification
{
    CodeAnalysis.Analyzers.BatchMethodAnalyzer _analyzer;

    void Establish() => _analyzer = new();

    [Fact] void should_support_observes_only_on_batch_methods() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.ObservesOnlyOnBatchMethods).ShouldBeTrue();
    [Fact] void should_support_batch_method_must_observe_event_types() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.BatchMethodMustObserveEventTypes).ShouldBeTrue();
    [Fact] void should_support_batch_context_only_on_batch_methods() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.BatchContextOnlyOnBatchMethods).ShouldBeTrue();
    [Fact] void should_support_reactor_batch_method_must_return_task_or_void() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.ReactorBatchMethodMustReturnTaskOrVoid).ShouldBeTrue();
    [Fact] void should_support_batch_method_signature_must_match_allowed() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.BatchMethodSignatureMustMatchAllowed).ShouldBeTrue();
}
