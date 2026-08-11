// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DuplicateSetFromContextAnalyzer.when_analyzing_a_read_model;

/// <summary>
/// A read model does not have to be positional. A declared property has no constructor parameter to pair with,
/// so it is analyzed on its own attributes - the path that must survive pairing the positional ones off.
/// </summary>
public class and_the_member_is_a_declared_property : given.a_duplicate_set_from_context_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class AccountOpened
    {
    }

    public record AccountAudit
    {
        public Guid Id { get; init; }

        [SetFromContext<AccountOpened>("SequenceNumber")]
        [SetFromContext<AccountOpened>("Occurred")]
        public DateTimeOffset {|#0:Stamp|} { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DuplicateSetFromContextAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.DuplicateSetFromContextForSameEventType, DiagnosticSeverity.Warning, "Stamp", "AccountOpened"));

    [Fact] Task should_report_the_discarded_mapping() => _result;
}
