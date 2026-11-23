// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DuplicateSetFromContextAnalyzer.when_analyzing_a_read_model;

public class and_one_member_maps_context_and_event_source_id_for_the_same_event : given.a_duplicate_set_from_context_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class AccountOpened
    {
    }

    public record AccountAudit
    {
        [SetFromContext<AccountOpened>("CorrelationId")]
        [SetFromEventSourceId<AccountOpened>]
        public Guid {|#0:Id|} { get; init; }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DuplicateSetFromContextAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.DuplicateSetFromContextForSameEventType, DiagnosticSeverity.Warning, "Id", "AccountOpened"));

    [Fact] Task should_report_the_discarded_mapping() => _result;
}
