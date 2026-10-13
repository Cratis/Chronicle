// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelPropertyMustHaveMappingSourceAnalyzer.when_analyzing;

/// <summary>
/// A <c language="csharp">[GlobalFor]</c> type maps a member for every variant of its group, so a variant property that only the shared
/// handler populates has a mapping source even though the variant itself declares none.
/// </summary>
public class and_property_is_mapped_by_a_shared_handler_of_a_variant_group : given.a_read_model_property_must_have_mapping_source_analyzer
{
    const string Usage = """
    public record IssueCreated(string Reference);

    public record TitleChanged(string Title);

    public class WorkItem;

    [VariantOf<WorkItem>]
    [EntersOn<IssueCreated>]
    public record BacklogItem([property: Key] Guid Id, string Title);

    [GlobalFor<WorkItem>]
    public record SharedHandlers([property: SetFrom<TitleChanged>] string Title);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelPropertyMustHaveMappingSourceAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
