// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelPropertyMustHaveMappingSourceAnalyzer.when_analyzing;

/// <summary>
/// A nested projection object is populated through its own mappings, not by AutoMap binding a same-named property on
/// the parent's events. Saying it will never be populated is simply untrue.
/// </summary>
public class and_property_is_a_nested_projection : given.a_read_model_property_must_have_mapping_source_analyzer
{
    const string Usage = """
    public record Registered(string Name);

    public record Address(string Street);

    [FromEvent<Registered>]
    public record Account(Guid Id, string Name, [Nested] Address? Address);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelPropertyMustHaveMappingSourceAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
