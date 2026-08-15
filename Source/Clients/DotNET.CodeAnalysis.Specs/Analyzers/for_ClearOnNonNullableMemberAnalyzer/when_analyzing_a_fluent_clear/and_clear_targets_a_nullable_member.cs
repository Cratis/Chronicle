// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ClearOnNonNullableMemberAnalyzer.when_analyzing_a_fluent_clear;

/// <summary>
/// The declaration the formal Clear exists for must never be reported.
/// </summary>
public class and_clear_targets_a_nullable_member : given.a_fluent_clear_on_non_nullable_member_analyzer
{
    const string Usage = """
    public record SliceCommandCleared();

    public record Slice(string Id, string? Command);

    public class SliceProjection : IProjectionFor<Slice>
    {
        public void Define(IProjectionBuilderFor<Slice> builder) => builder
            .From<SliceCommandCleared>(_ => _
                .Clear(m => m.Command));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ClearOnNonNullableMemberAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
