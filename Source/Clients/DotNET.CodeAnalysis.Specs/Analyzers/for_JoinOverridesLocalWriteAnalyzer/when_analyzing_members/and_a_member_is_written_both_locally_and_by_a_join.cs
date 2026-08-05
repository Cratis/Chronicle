// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_JoinOverridesLocalWriteAnalyzer.when_analyzing_members;

public class and_a_member_is_written_both_locally_and_by_a_join : given.a_join_overrides_local_write_analyzer
{
    const string Usage = """
    public record SideReleased(Guid RequestId);
    public record RequestLost(bool IsContractRoundLive);

    public record ContractSide(
        [Key] Guid Id,
        Guid RequestId,
        {|#0:[SetValue<SideReleased>(true)] [Join<RequestLost>(on: "RequestId")] bool IsContractRoundLive|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.JoinOverridesLocalWriteAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.JoinOverridesLocalWrite, DiagnosticSeverity.Warning, "IsContractRoundLive", "[SetValue<SideReleased>]", "RequestLost"));

    [Fact] Task should_report_the_join_overrides_local_write_diagnostic() => _result;
}
