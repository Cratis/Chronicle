// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DuplicateSetFromContextAnalyzer.when_analyzing_a_read_model;

/// <summary>
/// Pairing a parameter with the property generated from it is an identity, not a resemblance: the generated
/// property carries the parameter's name character for character. A declared property whose name differs from a
/// parameter only by casing is a different member, and must keep being analyzed on its own attributes.
/// </summary>
/// <remarks>
/// This is the spec that fixes which direction the pairing judges. Relaxing the comparison to ignore casing
/// pairs <c>Stamp</c> off against <c>stamp</c>, drops it from its own path, and then finds nothing under the
/// parameter's name either - the report disappears entirely, silently, with every other spec still green.
/// </remarks>
public class and_a_property_and_a_parameter_differ_only_by_casing : given.a_duplicate_set_from_context_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class AccountOpened
    {
    }

    public record AccountAudit(DateTimeOffset stamp)
    {
        [SetFromContext<AccountOpened>("SequenceNumber")]
        [SetFromContext<AccountOpened>("Occurred")]
        public DateTimeOffset {|#0:Stamp|} { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DuplicateSetFromContextAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.DuplicateSetFromContextForSameEventType, DiagnosticSeverity.Warning, "Stamp", "AccountOpened"));

    [Fact] Task should_report_the_discarded_mapping_on_the_declared_property() => _result;
}
