// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ClearOnNonNullableMemberAnalyzer.when_analyzing_a_fluent_clear;

/// <summary>
/// The control for the ToValue specs: a real constant is a set rather than a clear, whatever the member's
/// nullability - including the empty string this rule points authors at. Without it the ToValue specs could just as
/// well be reporting every ToValue on a non-nullable member.
/// </summary>
public class and_to_value_carries_a_real_constant : given.a_fluent_clear_on_non_nullable_member_analyzer
{
    const string Usage = """
    public record SliceCommandCleared();

    public record Slice(string Id, string Command);

    public class SliceProjection : IProjectionFor<Slice>
    {
        public void Define(IProjectionBuilderFor<Slice> builder) => builder
            .From<SliceCommandCleared>(_ => _
                .Set(m => m.Command).ToValue(""));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ClearOnNonNullableMemberAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
