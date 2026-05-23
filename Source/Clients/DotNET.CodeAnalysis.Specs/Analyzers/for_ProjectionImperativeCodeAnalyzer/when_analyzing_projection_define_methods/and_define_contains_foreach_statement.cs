// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ProjectionImperativeCodeAnalyzer.when_analyzing_projection_define_methods;

public class and_define_contains_foreach_statement : given.a_projection_imperative_code_analyzer
{
    const string Usage = """
    using System.Collections.Generic;

    public class AnEvent { }
    public class ReadModel { }

    public class MyProjection : Cratis.Chronicle.Projections.IProjectionFor<ReadModel>
    {
        static readonly IEnumerable<string> _items = new List<string>();

        public void Define(Cratis.Chronicle.Projections.IProjectionBuilderFor<ReadModel> builder)
        {
            {|#0:foreach (var item in _items)
            {
                builder.From<AnEvent>();
            }|}
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ProjectionImperativeCodeAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ProjectionDefineMustNotContainImperativeCode, DiagnosticSeverity.Error, "MyProjection"));

    [Fact] Task should_report_imperative_code_diagnostic() => _result;
}

