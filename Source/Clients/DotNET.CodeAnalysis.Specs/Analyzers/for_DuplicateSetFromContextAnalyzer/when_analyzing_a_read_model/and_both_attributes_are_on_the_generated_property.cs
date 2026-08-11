// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DuplicateSetFromContextAnalyzer.when_analyzing_a_read_model;

/// <summary>
/// The third placement a positional record allows: both attributes targeted at the generated property rather
/// than the parameter. It is the same discarded mapping, and it stays reported exactly once now that the
/// parameter carries the union - a member must not be counted from both of its symbols.
/// </summary>
public class and_both_attributes_are_on_the_generated_property : given.a_duplicate_set_from_context_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class AccountOpened
    {
    }

    public record AccountAudit(
        Guid Id,

        [property: SetFromContext<AccountOpened>("SequenceNumber")]
        [property: SetFromContext<AccountOpened>("Occurred")]
        {|#0:DateTimeOffset Stamp|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DuplicateSetFromContextAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.DuplicateSetFromContextForSameEventType, DiagnosticSeverity.Warning, "Stamp", "AccountOpened"));

    [Fact] Task should_report_the_discarded_mapping_once() => _result;
}
