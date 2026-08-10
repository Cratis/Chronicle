// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_CrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

/// <summary>
/// Even an identically named [Subject] can be null, empty, overridden at append, or differ in historical stored events.
/// </summary>
public class and_the_joined_event_declares_the_same_subject : given.a_cross_subject_pii_join_analyzer
{
    const string Usage = """
    public record AdvisorNamed([Subject] Guid AdvisorId, [PII] string FullName);

    public record AdvisorSummary(
        [Key] Guid AdvisorId,
        {|#0:[Join<AdvisorNamed>(on: "AdvisorId")] string FullName|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.CrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.CrossSubjectPiiJoin, DiagnosticSeverity.Error, "FullName", "AdvisorNamed", "AdvisorId"));

    [Fact] Task should_report_the_cross_subject_pii_join_diagnostic() => _result;
}
