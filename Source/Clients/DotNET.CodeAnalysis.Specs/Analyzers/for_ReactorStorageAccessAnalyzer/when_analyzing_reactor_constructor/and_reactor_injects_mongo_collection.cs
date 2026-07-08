// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorStorageAccessAnalyzer.when_analyzing_reactor_constructor;

public class and_reactor_injects_mongo_collection : given.a_reactor_storage_access_analyzer
{
    const string Usage = """
    public class SomeDocument
    {
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        public Reactor({|#0:MongoDB.Driver.IMongoCollection<SomeDocument> collection|})
        {
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorStorageAccessAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReactorMustNotAccessStorageDirectly, DiagnosticSeverity.Warning, "Reactor", "collection"));

    [Fact] Task should_report_diagnostic() => _result;
}
