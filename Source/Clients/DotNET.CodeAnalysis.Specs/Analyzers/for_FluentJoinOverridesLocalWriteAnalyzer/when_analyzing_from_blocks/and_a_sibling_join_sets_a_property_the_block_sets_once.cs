// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_FluentJoinOverridesLocalWriteAnalyzer.when_analyzing_from_blocks;

public class and_a_sibling_join_sets_a_property_the_block_sets_once : given.a_fluent_join_overrides_local_write_analyzer
{
    const string Usage = """
    public record SideOpened(Guid RequestId, string Owner);
    public record OwnerChanged(Guid RequestId, string Owner);

    public record ContractSide(Guid Id, Guid RequestId, string Owner);

    public class ContractSideProjection : IProjectionFor<ContractSide>
    {
        public void Define(IProjectionBuilderFor<ContractSide> builder) => builder
            .From<SideOpened>(b => b.SetOnce({|#0:m => m.Owner|}).To(e => e.Owner))
            .Join<OwnerChanged>(j => j.On(m => m.RequestId).Set(m => m.Owner).To(e => e.Owner));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.FluentJoinOverridesLocalWriteAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.JoinOverridesLocalWrite, DiagnosticSeverity.Warning, "Owner", "From<SideOpened>", "OwnerChanged"));

    [Fact] Task should_report_the_join_overrides_local_write_diagnostic() => _result;
}
