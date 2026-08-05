// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_JoinOverridesLocalWriteAnalyzer.when_analyzing_members;

public class and_the_join_stands_alone : given.a_join_overrides_local_write_analyzer
{
    const string Usage = """
    public record RequestLost(bool IsContractRoundLive);

    public record ContractSide(
        [Key] Guid Id,
        Guid RequestId,
        [Join<RequestLost>(on: "RequestId")] bool IsContractRoundLive);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.JoinOverridesLocalWriteAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}
