// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.given;

public class a_unit_of_work_with_two_events_for_different_event_source_ids_added_to_it : a_unit_of_work
{
    protected EventSourceId _firstEventEventSourceId;
    protected EventSourceId _secondEventEventSourceId;
    protected Causation _firstEventCausation;
    protected Causation _secondEventCausation;
    protected FirstEvent _firstEvent;
    protected SecondEvent _secondEvent;

    void Establish()
    {
        _firstEventEventSourceId = EventSourceId.New();
        _secondEventEventSourceId = EventSourceId.New();
        _firstEvent = new();
        _secondEvent = new();
        _firstEventCausation = new Causation(DateTimeOffset.UtcNow, "firstEventCausation", new Dictionary<string, string>());
        _secondEventCausation = new Causation(DateTimeOffset.UtcNow, "secondEventCausation", new Dictionary<string, string>());

        _unitOfWork.AddEvent(EventSequenceId.Log, _firstEventEventSourceId, _firstEvent, _firstEventCausation);
        _unitOfWork.AddEvent(EventSequenceId.Log, _secondEventEventSourceId, _secondEvent, _secondEventCausation);
    }


    protected record FirstEvent();
    protected record SecondEvent();
}
