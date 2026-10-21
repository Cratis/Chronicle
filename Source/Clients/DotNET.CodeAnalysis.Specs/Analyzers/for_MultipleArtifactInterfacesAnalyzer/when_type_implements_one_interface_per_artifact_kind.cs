// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MultipleArtifactInterfacesAnalyzer;

public class when_type_implements_one_interface_per_artifact_kind : given.an_analyzer
{
    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.MultipleArtifactInterfacesAnalyzer>.VerifyAnalyzer(
        CreateSource("public class Artifact : Cratis.Chronicle.Projections.IProjectionFor<First>, Cratis.Chronicle.Reducers.IReducerFor<Second> { }"));

    [Fact] Task should_not_report_multiple_interfaces() => _result;
}
