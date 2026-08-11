// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DuplicateSetFromContextAnalyzer.when_analyzing_a_read_model;

/// <summary>
/// On a positional record the parameter and the generated property are two distinct symbols, so the same
/// duplicate survives split across them: the builder's parameter pass and property pass each write the same
/// property key into the same event type's definition, and the property pass runs second and wins. The two
/// passes are disjoint per symbol - which is exactly what a per-symbol grouping cannot see.
/// </summary>
public class and_the_attributes_are_split_between_parameter_and_property : given.a_duplicate_set_from_context_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class AccountOpened
    {
    }

    public record AccountAudit(
        Guid Id,

        [SetFromContext<AccountOpened>("SequenceNumber")]
        [property: SetFromContext<AccountOpened>("Occurred")]
        {|#0:DateTimeOffset Stamp|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DuplicateSetFromContextAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.DuplicateSetFromContextForSameEventType, DiagnosticSeverity.Warning, "Stamp", "AccountOpened"));

    [Fact] Task should_report_the_discarded_mapping() => _result;
}
