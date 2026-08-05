// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DuplicateSetFromContextAnalyzer.when_analyzing_a_read_model;

/// <summary>
/// The attribute allows multiple deliberately, and this is what for: one property capturing context from several
/// distinct event types, each landing in its own definition. Reporting it would break the supported case.
/// </summary>
public class and_the_event_types_are_distinct : given.a_duplicate_set_from_context_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class AccountOpened
    {
    }

    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public class AccountClosed
    {
    }

    public record AccountAudit(
        Guid Id,

        [SetFromContext<AccountOpened>("Occurred")]
        [SetFromContext<AccountClosed>("Occurred")]
        DateTimeOffset LastTouched);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DuplicateSetFromContextAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_report_nothing() => _result;
}
