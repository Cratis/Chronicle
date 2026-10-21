// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelPropertyMustHaveMappingSourceAnalyzer.when_analyzing;

/// <summary>
/// AutoMap binds a variant's property from its entering event, so the entering event counts as subscribed.
/// </summary>
public class and_property_is_carried_by_the_entering_event_of_a_variant : given.a_read_model_property_must_have_mapping_source_analyzer
{
    const string Usage = """
    public record IssueCreated(string Title);

    public class WorkItem;

    [VariantOf<WorkItem>]
    [EntersOn<IssueCreated>]
    public record BacklogItem([property: Key] Guid Id, string Title);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelPropertyMustHaveMappingSourceAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
