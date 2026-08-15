// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ClearOnNonNullableMemberAnalyzer.when_analyzing_a_clear_declaration;

/// <summary>
/// A reference type in a file that opted out of nullable analysis promises nothing. Treating that silence as a
/// promise of non-null would report a declaration the compiler itself has no opinion on, so it is left alone -
/// and the projection builder makes the same call at runtime.
/// </summary>
public class and_the_member_is_oblivious_to_nullable_analysis : given.a_clear_on_non_nullable_member_analyzer
{
    const string Usage = """
    public record SliceCommandCleared();

    public record Slice(
        [Key] string Id,
        [ClearWith<SliceCommandCleared>] string Command);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ClearOnNonNullableMemberAnalyzer>.VerifyAnalyzer(CreateSource(Usage, nullableEnabled: false));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
