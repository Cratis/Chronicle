// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelPropertyMustHaveMappingSourceAnalyzer.when_analyzing;

public class and_property_is_explicitly_sourced : given.a_read_model_property_must_have_mapping_source_analyzer
{
    const string Usage = """
    public record Registered(Guid Reference);

    [FromEvent<Registered>]
    public record Account(
        Guid Id,
        [SetFrom<Registered>(nameof(Registered.Reference))] string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelPropertyMustHaveMappingSourceAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
