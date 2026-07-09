// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyOrSubjectOnEventSourceIdAnalyzer.when_analyzing_members;

public class and_key_on_event_source_id_parameter : given.a_key_or_subject_on_event_source_id_analyzer
{
    const string Usage = """
    public record AccountId(Guid Value) : EventSourceId<Guid>(Value);

    public record Account(
        {|#0:[Key]|} AccountId Id,
        string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyOrSubjectOnEventSourceIdAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.KeyOrSubjectOnEventSourceId, DiagnosticSeverity.Warning, "Id", "AccountId", "Key"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
