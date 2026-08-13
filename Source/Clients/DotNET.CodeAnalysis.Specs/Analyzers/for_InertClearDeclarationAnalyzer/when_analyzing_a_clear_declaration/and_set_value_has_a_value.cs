// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertClearDeclarationAnalyzer.when_analyzing_a_clear_declaration;

/// <summary>
/// A real constant produces a real mapping and must never be reported.
/// </summary>
public class and_set_value_has_a_value : given.an_inert_clear_declaration_analyzer
{
    const string Usage = """
    public record SliceCommandCleared();

    public record Slice(
        [Key] string Id,
        [SetValue<SliceCommandCleared>("")] string Command);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertClearDeclarationAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
