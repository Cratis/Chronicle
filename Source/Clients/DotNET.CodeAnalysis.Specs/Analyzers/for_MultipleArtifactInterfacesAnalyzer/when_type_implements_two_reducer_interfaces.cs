// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MultipleArtifactInterfacesAnalyzer;

public class when_type_implements_two_reducer_interfaces : given.an_analyzer
{
    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.MultipleArtifactInterfacesAnalyzer>.VerifyAnalyzer(
        CreateSource("{|#0:public class Reducer|} : Cratis.Chronicle.Reducers.IReducerFor<First>, Cratis.Chronicle.Reducers.IReducerFor<Second> { }"),
        new ExpectedDiagnostic(DiagnosticIds.MultipleArtifactInterfaces, DiagnosticSeverity.Warning, "Reducer", "IReducerFor", "Sample.First", "Sample.Second"));

    [Fact] Task should_report_multiple_reducer_interfaces() => _result;
}
