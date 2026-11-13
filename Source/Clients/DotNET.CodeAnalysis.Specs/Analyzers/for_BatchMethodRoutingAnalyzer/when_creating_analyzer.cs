// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodRoutingAnalyzer;

public class when_creating_analyzer : Specification
{
    CodeAnalysis.Analyzers.BatchMethodRoutingAnalyzer _analyzer;

    void Establish() => _analyzer = new();

    [Fact] void should_support_event_type_must_reach_one_method() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.EventTypeMustReachOneMethod).ShouldBeTrue();
    [Fact] void should_support_reducer_batch_method_must_be_its_only_method() => _analyzer.SupportedDiagnostics.Any(_ => _.Id == DiagnosticIds.ReducerBatchMethodMustBeItsOnlyMethod).ShouldBeTrue();
}
