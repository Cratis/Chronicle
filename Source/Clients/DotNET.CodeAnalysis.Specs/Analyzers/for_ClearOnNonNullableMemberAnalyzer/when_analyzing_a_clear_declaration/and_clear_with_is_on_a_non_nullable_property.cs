// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ClearOnNonNullableMemberAnalyzer.when_analyzing_a_clear_declaration;

/// <summary>
/// A non-nullable reference type has no "no value" state, so the clear has nothing correct to write.
/// </summary>
public class and_clear_with_is_on_a_non_nullable_property : given.a_clear_on_non_nullable_member_analyzer
{
    const string Usage = """
    public record SliceCommandCleared();

    public class Slice
    {
        [Key] public string Id { get; set; } = string.Empty;

        [{|#0:ClearWith<SliceCommandCleared>|}] public string Command { get; set; } = string.Empty;
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ClearOnNonNullableMemberAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ClearOnNonNullableMember, DiagnosticSeverity.Warning, "Command", "string"));

    [Fact] Task should_report_the_clear_on_non_nullable_member_diagnostic() => _result;
}
