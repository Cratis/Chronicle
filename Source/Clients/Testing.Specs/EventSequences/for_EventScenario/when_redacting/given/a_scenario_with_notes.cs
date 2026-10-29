// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_redacting.given;

public class a_scenario_with_notes : Specification
{
    protected EventScenario _scenario;
    protected EventSourceId _redactedSource;
    protected EventSourceId _otherSource;
    protected EventSequenceNumber _firstNote;
    protected EventSequenceNumber _tag;
    protected EventSequenceNumber _secondNote;
    protected EventSequenceNumber _otherNote;
    protected EventTypeId _noteType;
    protected EventTypeId _tagType;

    async Task Establish()
    {
        _scenario = new EventScenario();
        _noteType = Defaults.Instance.EventTypes.GetEventTypeFor(typeof(RedactableNoteWritten)).Id;
        _tagType = Defaults.Instance.EventTypes.GetEventTypeFor(typeof(RedactableNoteTagged)).Id;
        _redactedSource = EventSourceId.New();
        _otherSource = EventSourceId.New();
        _firstNote = (await _scenario.EventLog.Append(_redactedSource, new RedactableNoteWritten("first"))).SequenceNumber;
        _tag = (await _scenario.EventLog.Append(_redactedSource, new RedactableNoteTagged("pinned"))).SequenceNumber;
        _secondNote = (await _scenario.EventLog.Append(_redactedSource, new RedactableNoteWritten("second"))).SequenceNumber;
        _otherNote = (await _scenario.EventLog.Append(_otherSource, new RedactableNoteWritten("other"))).SequenceNumber;
    }

    void Destroy() => _scenario.Dispose();
}
