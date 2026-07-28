// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_CrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

public class and_the_joined_event_property_is_marked_pii : given.a_cross_subject_pii_join_analyzer
{
    const string Usage = """
    public record AdvisorNamed([PII] string FullName);

    public record RequestSummary(
        [Key] Guid Id,
        Guid AdvisorId,
        {|#0:[Join<AdvisorNamed>(on: "AdvisorId")] string FullName|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.CrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.CrossSubjectPiiJoin, DiagnosticSeverity.Error, "FullName", "AdvisorNamed", "AdvisorId"));

    [Fact] Task should_report_the_cross_subject_pii_join_diagnostic() => _result;
}
