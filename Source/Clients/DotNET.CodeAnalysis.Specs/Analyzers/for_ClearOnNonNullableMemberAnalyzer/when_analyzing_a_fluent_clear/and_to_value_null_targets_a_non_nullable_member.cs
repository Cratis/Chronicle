// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ClearOnNonNullableMemberAnalyzer.when_analyzing_a_fluent_clear;

/// <summary>
/// ToValue(null) is the same clear as Clear, so it answers to the same rule. This was the one path with no gate at
/// all: it neither warned nor threw, and before the scalar clear existed it wrote the literal string "null".
/// </summary>
public class and_to_value_null_targets_a_non_nullable_member : given.a_fluent_clear_on_non_nullable_member_analyzer
{
    const string Usage = """
    public record SliceCommandCleared();

    public record Slice(string Id, string Command);

    public class SliceProjection : IProjectionFor<Slice>
    {
        public void Define(IProjectionBuilderFor<Slice> builder) => builder
            .From<SliceCommandCleared>(_ => _
                {|#0:.Set(m => m.Command).ToValue(null!)|});
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ClearOnNonNullableMemberAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ClearOnNonNullableMember, DiagnosticSeverity.Warning, "Command", "string"));

    [Fact] Task should_report_the_clear_on_non_nullable_member_diagnostic() => _result;
}
