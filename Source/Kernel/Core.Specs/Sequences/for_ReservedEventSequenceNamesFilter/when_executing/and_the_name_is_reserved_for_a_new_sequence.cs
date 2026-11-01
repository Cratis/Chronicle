// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceNamesFilter.when_executing;

public class and_the_name_is_reserved_for_a_new_sequence : given.a_reserved_event_sequence_names_filter
{
    CommandContext _context;
    CommandResult _result;

    void Establish()
    {
        Reserve("observers");
        _context = ContextFor(AppendTo("observers"));
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_one_error() => _result.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_report_the_event_sequence_member() => _result.ValidationResults.Single().Members.ShouldContain("eventSequenceId");
    [Fact] void should_name_the_reserved_event_sequence() => _result.ValidationResults.Single().Message.ShouldEqual(ReservedEventSequenceRules.Message("observers"));
    [Fact] void should_carry_the_correlation_id() => _result.CorrelationId.ShouldEqual(_context.CorrelationId);
}
