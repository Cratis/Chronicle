// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentJoinOverridesLocalWriteAnalyzer.when_analyzing_from_blocks;

public class and_no_join_exists : given.a_fluent_join_overrides_local_write_analyzer
{
    const string Usage = """
    public record SideReleased(Guid RequestId);

    public record ContractSide(Guid Id, Guid RequestId, bool IsContractRoundLive);

    public class ContractSideProjection : IProjectionFor<ContractSide>
    {
        public void Define(IProjectionBuilderFor<ContractSide> builder) => builder
            .From<SideReleased>(b => b.Set(m => m.IsContractRoundLive).ToValue(true));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentJoinOverridesLocalWriteAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}
