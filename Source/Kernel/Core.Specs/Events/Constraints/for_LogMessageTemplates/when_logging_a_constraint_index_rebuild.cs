// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints.for_LogMessageTemplates.given;

namespace Cratis.Chronicle.Events.Constraints.for_LogMessageTemplates;

public class when_logging_a_constraint_index_rebuild : Specification
{
    RecordingLogger _logger;
    string _started;
    string _finished;

    void Establish() => _logger = new();

    void Because()
    {
        _logger.ConstraintIndexRebuildStarted("some-store", "some-namespace", "some-sequence", "first, second", Guid.Empty);
        _started = _logger.Message;
        _logger.ConstraintIndexRebuildFinished("some-store", "some-namespace", "some-sequence", "first, second", Guid.Empty, true, TimeSpan.FromSeconds(3));
        _finished = _logger.Message;
    }

    [Fact] void should_identify_the_sequence_when_starting() => _started.ShouldContain("some-sequence");
    [Fact] void should_identify_the_constraints_when_starting() => _started.ShouldContain("first, second");
    [Fact] void should_identify_the_sequence_when_finished() => _finished.ShouldContain("some-sequence");
    [Fact] void should_identify_the_constraints_when_finished() => _finished.ShouldContain("first, second");
    [Fact] void should_report_the_duration_when_finished() => _finished.ShouldContain("00:00:03");
}
