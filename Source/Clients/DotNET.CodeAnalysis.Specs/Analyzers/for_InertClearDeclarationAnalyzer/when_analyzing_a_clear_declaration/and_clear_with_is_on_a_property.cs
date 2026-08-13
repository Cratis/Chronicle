// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertClearDeclarationAnalyzer.when_analyzing_a_clear_declaration;

/// <summary>
/// A property-level ClearWith binds to nothing, whatever the property holds.
/// </summary>
public class and_clear_with_is_on_a_property : given.an_inert_clear_declaration_analyzer
{
    const string Usage = """
    public record SliceCommandCleared();

    public class Slice
    {
        [Key] public string Id { get; set; } = string.Empty;

        [{|#0:ClearWith<SliceCommandCleared>|}] public string? Command { get; set; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertClearDeclarationAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.InertClearDeclaration, DiagnosticSeverity.Warning, "ClearWith<SliceCommandCleared>", "[ClearWith] is only read from the class-level attributes of a nested single-object type, never from a property or a parameter"));

    [Fact] Task should_report_the_inert_clear_declaration_diagnostic() => _result;
}
