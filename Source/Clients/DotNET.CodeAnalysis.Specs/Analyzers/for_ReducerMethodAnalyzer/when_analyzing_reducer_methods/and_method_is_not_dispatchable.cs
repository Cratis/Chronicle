// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMethodAnalyzer.when_analyzing_reducer_methods;

public class and_method_is_not_dispatchable : given.a_reducer_method_analyzer
{
    [Theory]
    [InlineData("Model", "Model current, object @event", DiagnosticIds.ReducerMethodSignatureMustMatchAllowed)]
    [InlineData("Model", "object @event, Model current", DiagnosticIds.ReducerEventParameterMustHaveAttribute)]
    [InlineData("Task<Model>", "KnownEvent @event, Model current, string context", DiagnosticIds.ReducerMethodSignatureMustMatchAllowed)]
    [InlineData("void", "KnownEvent @event, Model current", DiagnosticIds.ReducerMethodSignatureMustMatchAllowed)]
    [InlineData("Model", "KnownEvent @event", DiagnosticIds.ReducerMethodSignatureMustMatchAllowed)]
    [InlineData("Model", "KnownEvent @event, ref Model current", DiagnosticIds.ReducerMethodSignatureMustMatchAllowed)]
    public Task should_report_skipped_shape(string returnType, string parameters, string diagnosticId)
    {
        var isEventDiagnostic = diagnosticId == DiagnosticIds.ReducerEventParameterMustHaveAttribute;
        var markedParameters = isEventDiagnostic ? "{|#0:object @event|}, Model current" : parameters;
        var name = isEventDiagnostic ? "Reduce" : "{|#0:Reduce|}";
        var source = CreateSource($$"""
        [EventType] public class KnownEvent {}
        public class Model {}
        public class Reducer : Cratis.Chronicle.Reducers.IReducerFor<Model>
        {
            public {{returnType}} {{name}}({{markedParameters}}) => throw new NotImplementedException();
        }
        """);
        var expected = isEventDiagnostic
            ? new ExpectedDiagnostic(diagnosticId, DiagnosticSeverity.Error, "Object", "Reduce")
            : new ExpectedDiagnostic(diagnosticId, DiagnosticSeverity.Warning, "Reduce");
        return AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMethodAnalyzer>.VerifyAnalyzer(source, expected);
    }
}
