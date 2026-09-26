// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_reading_detached;

public class and_initial_state_has_no_folded_events : given.a_decision_reader
{
    DecisionRead<Model> _read;

    void Establish()
    {
        _last = EventSequenceNumber.Unavailable.Value;
        _probe = EventSequenceNumber.Unavailable.Value;
        _json = "{\"id\":\"initial-default\"}";
    }

    async Task Because() => _read = await _reader.GetDetached<Model>("source");

    [Fact] void should_read_as_absent() => _read.Exists.ShouldBeFalse();
}
