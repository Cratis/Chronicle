// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentJoinOverridesLocalWriteAnalyzer.when_analyzing_from_blocks;

public class and_a_sibling_join_sets_the_same_property : given.a_fluent_join_overrides_local_write_analyzer
{
    const string Usage = """
    public record SideReleased(Guid RequestId);
    public record RequestLost(Guid RequestId);

    public record ContractSide(Guid Id, Guid RequestId, bool IsContractRoundLive);

    public class ContractSideProjection : IProjectionFor<ContractSide>
    {
        public void Define(IProjectionBuilderFor<ContractSide> builder) => builder
            .From<SideReleased>(b => b.Set({|#0:m => m.IsContractRoundLive|}).ToValue(true))
            .Join<RequestLost>(j => j.On(m => m.RequestId).Set(m => m.IsContractRoundLive).ToValue(false));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentJoinOverridesLocalWriteAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.JoinOverridesLocalWrite, DiagnosticSeverity.Warning, "IsContractRoundLive", "From<SideReleased>", "RequestLost"));

    [Fact] Task should_report_the_join_overrides_local_write_diagnostic() => _result;
}
