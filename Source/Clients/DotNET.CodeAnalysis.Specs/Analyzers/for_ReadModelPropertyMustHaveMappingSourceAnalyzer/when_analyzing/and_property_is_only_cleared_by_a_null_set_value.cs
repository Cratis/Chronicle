// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelPropertyMustHaveMappingSourceAnalyzer.when_analyzing;

/// <summary>
/// The two spellings of a clear are held to one rule. A null [SetValue] used to suppress this diagnostic on the
/// strength of being a [SetValue] at all, which hid a member that one event cleared and nothing ever populated.
/// </summary>
public class and_property_is_only_cleared_by_a_null_set_value : given.a_read_model_property_must_have_mapping_source_analyzer
{
    const string Usage = """
    public record Registered(Guid Reference);

    public record Cancelled();

    [FromEvent<Registered>]
    public record Account(Guid Id, [SetValue<Cancelled>(null)] {|#0:string? Name|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelPropertyMustHaveMappingSourceAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReadModelPropertyMustHaveMappingSource, DiagnosticSeverity.Warning, "Name"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
