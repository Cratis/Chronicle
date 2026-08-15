// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelPropertyMustHaveMappingSourceAnalyzer.when_analyzing;

/// <summary>
/// A clear says what returns a member to no value, never where a value comes from. A member declaring only a clear
/// has no mapping source and is reported, exactly as one declaring nothing at all would be.
/// </summary>
public class and_property_is_only_cleared : given.a_read_model_property_must_have_mapping_source_analyzer
{
    const string Usage = """
    public record Registered(Guid Reference);

    public record Cancelled();

    [FromEvent<Registered>]
    public record Account(Guid Id, [ClearWith<Cancelled>] {|#0:string? Name|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelPropertyMustHaveMappingSourceAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReadModelPropertyMustHaveMappingSource, DiagnosticSeverity.Warning, "Name"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
