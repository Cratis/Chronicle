// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_CrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

public class and_the_join_is_on_the_read_models_own_subject : given.a_cross_subject_pii_join_analyzer
{
    const string Usage = """
    [PII]
    public record DisplayName(string Value);

    public record AdvisorNamed(DisplayName DisplayName);

    public record AdvisorSummary(
        [Key] Guid AdvisorId,
        {|#0:[Join<AdvisorNamed>(on: "AdvisorId", eventPropertyName: "DisplayName")] DisplayName Name|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.CrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.UnprovableCrossSubjectPiiJoin, DiagnosticSeverity.Warning, "Name", "AdvisorNamed", "DisplayName", "AdvisorId"));

    [Fact] Task should_report_the_unprovable_subject_warning() => _result;
}
