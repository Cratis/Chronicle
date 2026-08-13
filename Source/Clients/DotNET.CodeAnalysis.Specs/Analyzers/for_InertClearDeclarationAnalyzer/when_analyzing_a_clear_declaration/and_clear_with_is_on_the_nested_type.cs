// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertClearDeclarationAnalyzer.when_analyzing_a_clear_declaration;

/// <summary>
/// A class-level ClearWith on a nested type is the one shape projection construction reads.
/// </summary>
public class and_clear_with_is_on_the_nested_type : given.an_inert_clear_declaration_analyzer
{
    const string Usage = """
    public record SliceCommandCleared();

    [ClearWith<SliceCommandCleared>]
    public record SliceCommand(string Name);

    public record Slice(
        [Key] string Id,
        [Nested] SliceCommand? Command);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertClearDeclarationAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
