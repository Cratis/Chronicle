// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_KeyRedirectionPiiAnalyzer.when_analyzing_model_bound_projections;

/// <summary>
/// A declared control rather than a mutation target: CHR0038 owns the [Join] spelling and this rule must never grow into it.
/// </summary>
public class and_a_cross_subject_join_is_the_only_defect : given.a_key_redirection_pii_analyzer
{
    const string Usage = """
    public record AdvisorNamed([PII] string FullName);

    public record RequestSummary(
        [Key] string Id,
        string AdvisorId,
        [Join<AdvisorNamed>(on: "AdvisorId", eventPropertyName: "FullName")] string AdvisorName);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.KeyRedirectionPiiAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
