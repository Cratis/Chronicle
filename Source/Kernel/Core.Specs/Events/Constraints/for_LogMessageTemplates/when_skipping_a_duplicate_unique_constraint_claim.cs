// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints.for_LogMessageTemplates.given;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_LogMessageTemplates;

public class when_skipping_a_duplicate_unique_constraint_claim : Specification
{
    const string Constraint = "unique-name";
    const string Claimant = "claimant@example.com";
    RecordingLogger _logger;
    IReadOnlyList<KeyValuePair<string, object?>> _state;
    string _message;
    Exception _exception;

    void Establish() => _logger = new();

    void Because()
    {
        _logger.SkippingDuplicateUniqueConstraintClaim(Constraint, 42, new DuplicateUniqueConstraintValue(Constraint, Claimant));
        _state = _logger.State;
        _message = _logger.Message;
        _exception = _logger.Exception!;
    }

    [Fact] void should_identify_the_constraint() => _message.ShouldContain(Constraint);
    [Fact] void should_identify_the_event_position() => _message.ShouldContain("sequence number 42");
    [Fact] void should_not_include_the_claimant_in_the_message() => _message.ShouldNotContain(Claimant);
    [Fact] void should_not_include_the_claimant_in_the_exception() => _exception.ToString().ShouldNotContain(Claimant);
    [Fact] void should_not_include_the_event_source_id_in_structured_state() => _state.Any(entry => entry.Key == "EventSourceId" || entry.Value?.ToString()?.Contains(Claimant) == true).ShouldBeFalse();
    [Fact] void should_not_include_the_event_source_id_in_the_template() => _state.Single(entry => entry.Key == "{OriginalFormat}").Value!.ToString()!.ShouldNotContain("EventSourceId");
}
