// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorInjectionAnalyzer.when_analyzing_reactor_types;

public class and_reactor_does_not_inject_event_log : given.a_reactor_injection_analyzer
{
    const string Usage = """
    public interface ISomeService { }

    public class MyReactor : Cratis.Chronicle.Reactors.IReactor
    {
        public MyReactor(ISomeService service) { }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorInjectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}
