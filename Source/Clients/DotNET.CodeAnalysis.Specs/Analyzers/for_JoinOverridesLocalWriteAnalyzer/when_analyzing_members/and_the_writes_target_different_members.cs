// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_JoinOverridesLocalWriteAnalyzer.when_analyzing_members;

public class and_the_writes_target_different_members : given.a_join_overrides_local_write_analyzer
{
    const string Usage = """
    public record SideReleased(Guid RequestId);
    public record RequestLost(DateTimeOffset LostAt);

    public record ContractSide(
        [Key] Guid Id,
        Guid RequestId,
        [SetValue<SideReleased>(true)] bool IsContractRoundLive,
        [Join<RequestLost>(on: "RequestId")] DateTimeOffset LostAt);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.JoinOverridesLocalWriteAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}
