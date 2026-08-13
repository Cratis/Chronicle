// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertClearDeclarationAnalyzer.when_analyzing_a_clear_declaration;

/// <summary>
/// A null SetValue is dropped where the mappings are built, so the stale value stands.
/// </summary>
public class and_set_value_is_null_on_a_record_parameter : given.an_inert_clear_declaration_analyzer
{
    const string Usage = """
    public record SliceCommandCleared();

    public record Slice(
        [Key] string Id,
        [{|#0:SetValue<SliceCommandCleared>(null)|}] string? Command);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertClearDeclarationAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.InertClearDeclaration, DiagnosticSeverity.Warning, "SetValue<SliceCommandCleared>(null)", "a null value is skipped when the set-value mappings are built, so no mapping is emitted for the member at all"));

    [Fact] Task should_report_the_inert_clear_declaration_diagnostic() => _result;
}
