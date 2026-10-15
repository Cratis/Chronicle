// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConcurrencyWithoutEventSourceIdAnalyzer;

public class when_declared_concurrency_omits_the_source : given.concurrency_declarations
{
    Task _result;

    void Because() => _result = Task.WhenAll(
        new[] { "EventSource", "EventStream(\"Approval\", " }.SelectMany(attribute =>
            new[] { "Dimensions.EventStreamType", "Dimensions.EventStreamType | Dimensions.EventStreamId", "(Dimensions)4" }
                .Select(expression => AnalyzerVerifier<CodeAnalysis.Analyzers.ConcurrencyWithoutEventSourceIdAnalyzer>.VerifyAnalyzer(
                    Source($"[{(attribute == "EventSource" ? "EventSource(" : attribute)}Concurrency = {{|#0:{expression}|}})] public class Account : IEventSource {{ }}"),
                    new ExpectedDiagnostic(DiagnosticIds.ConcurrencyWithoutEventSourceId, DiagnosticSeverity.Info)))));

    [Fact] Task should_report_each_definition_and_stream_declaration() => _result;
}
