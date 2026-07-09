// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMutableStateAnalyzer.when_analyzing_reactor_state;

public class and_reactor_is_stateless : given.a_reactor_mutable_state_analyzer
{
    const string Usage = """
    public interface IDependency
    {
    }

    public class Reactor(IDependency dependency) : Cratis.Chronicle.Reactors.IReactor
    {
        readonly IDependency _dependency = dependency;

        public string Name { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMutableStateAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_diagnostic() => _result;
}
