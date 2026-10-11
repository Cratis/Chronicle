// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MultipleArtifactInterfacesAnalyzer;

public class when_type_inherits_an_artifact_interface : given.an_analyzer
{
    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.MultipleArtifactInterfacesAnalyzer>.VerifyAnalyzer(
        CreateSource("""
            public class BaseProjection : Cratis.Chronicle.Projections.IProjectionFor<First> { }
            {|#0:public class Projection|} : BaseProjection, Cratis.Chronicle.Projections.IProjectionFor<Second> { }
            """),
        new ExpectedDiagnostic(DiagnosticIds.MultipleArtifactInterfaces, DiagnosticSeverity.Warning, "Projection", "IProjectionFor", "Sample.First", "Sample.Second"));

    [Fact] Task should_report_the_inherited_and_declared_interfaces() => _result;
}
