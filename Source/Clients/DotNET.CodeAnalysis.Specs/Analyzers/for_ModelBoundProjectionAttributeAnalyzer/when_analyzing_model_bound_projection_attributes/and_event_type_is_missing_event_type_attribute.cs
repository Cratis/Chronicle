// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ModelBoundProjectionAttributeAnalyzer.when_analyzing_model_bound_projection_attributes;

public class and_event_type_is_missing_event_type_attribute : given.a_model_bound_projection_attribute_analyzer
{
    const string Usage = """
    public class MissingEvent
    {
    }

    {|#0:[FromEvent<MissingEvent>]|}
    public class Projection
    {
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ModelBoundProjectionAttributeAnalyzer>.VerifyAnalyzer(CreateSource(Usage), new ExpectedDiagnostic(DiagnosticIds.ModelBoundProjectionEventTypeMustHaveAttribute, DiagnosticSeverity.Error, "MissingEvent"));

    [Fact] Task should_report_event_type_attribute_diagnostic() => _result;
}
