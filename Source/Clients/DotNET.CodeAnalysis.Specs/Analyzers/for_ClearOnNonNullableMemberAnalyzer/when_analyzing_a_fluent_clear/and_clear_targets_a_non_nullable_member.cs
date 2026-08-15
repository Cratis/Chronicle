// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ClearOnNonNullableMemberAnalyzer.when_analyzing_a_fluent_clear;

/// <summary>
/// The fluent Clear is held to the same ruling as the attribute form. C# cannot refuse this at the signature - a
/// non-nullable argument converts to a nullable parameter without complaint - so the rule lives here.
/// </summary>
public class and_clear_targets_a_non_nullable_member : given.a_fluent_clear_on_non_nullable_member_analyzer
{
    const string Usage = """
    public record SliceCommandCleared();

    public record Slice(string Id, string Command);

    public class SliceProjection : IProjectionFor<Slice>
    {
        public void Define(IProjectionBuilderFor<Slice> builder) => builder
            .From<SliceCommandCleared>(_ => _
                {|#0:.Clear(m => m.Command)|});
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ClearOnNonNullableMemberAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ClearOnNonNullableMember, DiagnosticSeverity.Warning, "Command", "string"));

    [Fact] Task should_report_the_clear_on_non_nullable_member_diagnostic() => _result;
}
