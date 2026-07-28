// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_CrossSubjectPiiJoinAnalyzer.when_analyzing_joins;

public class and_the_joined_value_is_not_pii : given.a_cross_subject_pii_join_analyzer
{
    const string Usage = """
    public record AdvisorNamed(string Department);

    public record RequestSummary(
        [Key] Guid Id,
        Guid AdvisorId,
        [Join<AdvisorNamed>(on: "AdvisorId")] string Department);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.CrossSubjectPiiJoinAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
