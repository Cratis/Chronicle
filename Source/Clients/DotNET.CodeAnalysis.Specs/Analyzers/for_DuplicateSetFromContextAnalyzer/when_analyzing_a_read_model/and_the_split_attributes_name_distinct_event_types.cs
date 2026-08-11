// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DuplicateSetFromContextAnalyzer.when_analyzing_a_read_model;

/// <summary>
/// The paired guard for the split placement: unioning the parameter's attributes with the generated property's
/// must not turn the supported case into a report. Each event type gets its own definition, so the two writes
/// land in different places and neither is discarded - exactly as when both attributes sit on one symbol.
/// </summary>
public class and_the_split_attributes_name_distinct_event_types : given.a_duplicate_set_from_context_analyzer
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
        [property: SetFromContext<AccountClosed>("Occurred")]
        DateTimeOffset LastTouched);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DuplicateSetFromContextAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_report_nothing() => _result;
}
