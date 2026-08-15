// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelPropertyMustHaveMappingSourceAnalyzer.when_analyzing;

/// <summary>
/// The control for the two clear specs: a [SetValue] carrying a real constant is a mapping source and still
/// silences the rule, so what those specs measure is the null rather than the attribute.
/// </summary>
public class and_property_is_sourced_by_a_set_value_and_cleared : given.a_read_model_property_must_have_mapping_source_analyzer
{
    const string Usage = """
    public record Registered(Guid Reference);

    public record Cancelled();

    [FromEvent<Registered>]
    public record Account(Guid Id, [SetValue<Registered>("pending")] [ClearWith<Cancelled>] string? Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelPropertyMustHaveMappingSourceAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
