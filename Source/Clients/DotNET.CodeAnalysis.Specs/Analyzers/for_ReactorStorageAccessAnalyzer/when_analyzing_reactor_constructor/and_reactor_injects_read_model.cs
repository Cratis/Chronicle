// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorStorageAccessAnalyzer.when_analyzing_reactor_constructor;

public class and_reactor_injects_read_model : given.a_reactor_storage_access_analyzer
{
    const string Usage = """
    public interface IReadModels
    {
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        public Reactor(IReadModels readModels)
        {
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorStorageAccessAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_diagnostic() => _result;
}
