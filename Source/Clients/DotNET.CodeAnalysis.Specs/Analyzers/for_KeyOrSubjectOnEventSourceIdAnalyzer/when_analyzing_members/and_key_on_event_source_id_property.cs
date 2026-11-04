// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyOrSubjectOnEventSourceIdAnalyzer.when_analyzing_members;

public class and_key_on_event_source_id_property : given.a_key_or_subject_on_event_source_id_analyzer
{
    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyOrSubjectOnEventSourceIdAnalyzer>.VerifyAnalyzer(
        CreateSource("""
            public record OrderId(Guid Value) : EventSourceId<Guid>(Value);
            public record OrderPlaced({|#0:[property: Key]|} OrderId Id);
            """),
        new ExpectedDiagnostic(DiagnosticIds.KeyOrSubjectOnEventSourceId, DiagnosticSeverity.Warning, "Id", "OrderId", "Key"));

    [Fact] Task should_report_the_redundant_key() => _result;
}
