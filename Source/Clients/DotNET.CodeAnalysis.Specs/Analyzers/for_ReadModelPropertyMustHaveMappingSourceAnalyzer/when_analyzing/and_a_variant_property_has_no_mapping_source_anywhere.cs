// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelPropertyMustHaveMappingSourceAnalyzer.when_analyzing;

/// <summary>
/// A variant property that neither its entering event nor any shared handler populates is still reported.
/// </summary>
public class and_a_variant_property_has_no_mapping_source_anywhere : given.a_read_model_property_must_have_mapping_source_analyzer
{
    const string Usage = """
    public record IssueCreated(string Reference);

    public class WorkItem;

    [VariantOf<WorkItem>]
    [EntersOn<IssueCreated>]
    public record BacklogItem([property: Key] Guid Id, {|#0:string Title|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelPropertyMustHaveMappingSourceAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ReadModelPropertyMustHaveMappingSource, DiagnosticSeverity.Warning, "Title"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
