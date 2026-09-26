// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReadModelPropertyMustHaveMappingSourceAnalyzer.when_analyzing;

/// <summary>
/// A projection may only consume events, so a value joined in from a second read model can only be filled in by the
/// query that serves the model. Having no mapping source is the declared intent, not an oversight.
/// </summary>
public class and_property_is_assembled_by_the_reader : given.a_read_model_property_must_have_mapping_source_analyzer
{
    const string Usage = """
    public record Registered(string Name);

    public record Membership(string Level);

    [FromEvent<Registered>]
    public record Account(Guid Id, string Name, [NotProjected] IReadOnlyList<Membership> Memberships);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReadModelPropertyMustHaveMappingSourceAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
