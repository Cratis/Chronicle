// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Analyzers;
using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_BatchMethodAnalyzer.when_analyzing_batch_methods;

public class and_other_analyzers_see_valid_batch_methods : Specification
{
    static readonly string _source = BatchObserverSource.Create(and_the_batch_methods_are_valid.Source);

    [Fact] Task should_not_report_reactor_method_diagnostics() => AnalyzerVerifier<ReactorMethodAnalyzer>.VerifyAnalyzer(_source);
    [Fact] Task should_not_report_reducer_method_diagnostics() => AnalyzerVerifier<ReducerMethodAnalyzer>.VerifyAnalyzer(_source);
    [Fact] Task should_not_report_self_reducer_method_diagnostics() => AnalyzerVerifier<SelfReducerMethodAnalyzer>.VerifyAnalyzer(_source);
    [Fact] Task should_not_report_self_reducer_intent_diagnostics() => AnalyzerVerifier<SelfReducerIntentAnalyzer>.VerifyAnalyzer(_source);
    [Fact] Task should_not_report_self_reducer_ambiguity_diagnostics() => AnalyzerVerifier<SelfReducerAmbiguityAnalyzer>.VerifyAnalyzer(_source);
    [Fact] Task should_not_report_current_read_model_nullability_diagnostics() => AnalyzerVerifier<ReducerCurrentReadModelMustBeNullableAnalyzer>.VerifyAnalyzer(_source);
    [Fact] Task should_not_report_routing_diagnostics() => AnalyzerVerifier<BatchMethodRoutingAnalyzer>.VerifyAnalyzer(_source);
    [Fact] Task should_not_report_pattern_diagnostics() => AnalyzerVerifier<BatchMethodPatternAnalyzer>.VerifyAnalyzer(_source);
}
