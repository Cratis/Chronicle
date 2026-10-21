// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_MultipleArtifactInterfacesAnalyzer;

public class when_same_interface_is_inherited_twice : given.an_analyzer
{
    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.MultipleArtifactInterfacesAnalyzer>.VerifyAnalyzer(
        CreateSource("""
            public interface IFirst : Cratis.Chronicle.Reducers.IReducerFor<First> { }
            public interface ISecond : Cratis.Chronicle.Reducers.IReducerFor<First> { }
            public class Reducer : IFirst, ISecond { }
            """));

    [Fact] Task should_not_report_duplicate_paths_to_the_same_interface() => _result;
}
