// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DuplicateSetFromContextAnalyzer.when_analyzing_a_read_model;

/// <summary>
/// Both attributes map to the same property of the same event type's definition, so the last declared wins and
/// the earlier is discarded - with nothing at build, registration or runtime to say so. The property is
/// populated, with the other value, so nothing is null and nothing throws.
/// </summary>
public class and_one_member_names_the_same_event_type_twice : given.a_duplicate_set_from_context_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class AccountOpened
    {
    }

    public record AccountAudit(
        Guid Id,

        [SetFromContext<AccountOpened>("SequenceNumber")]
        [SetFromContext<AccountOpened>("Occurred")]
        {|#0:DateTimeOffset Stamp|});
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DuplicateSetFromContextAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.DuplicateSetFromContextForSameEventType, DiagnosticSeverity.Warning, "Stamp", "AccountOpened"));

    [Fact] Task should_report_the_discarded_mapping() => _result;
}
