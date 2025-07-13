// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

#nullable enable

namespace Cratis.Chronicle.Reducers;

public class SyncNullableReducer
{
    List<EventAndContext> _receivedEventsAndContexts = [];
    List<ReadModel?> _readModels = [];

    public IEnumerable<EventAndContext> ReceivedEventsAndContexts => _receivedEventsAndContexts;
    public IEnumerable<ValidEvent> ReceivedEvents => _receivedEventsAndContexts.Select(_ => _.Event).Cast<ValidEvent>().ToArray();
    public IEnumerable<EventContext> ReceivedEventContexts => _receivedEventsAndContexts.Select(_ => _.Context).ToArray();
    public IEnumerable<ReadModel?> ReceivedReadModels => _readModels;

    public ReadModel? Reduce(ValidEvent @event, ReadModel? initial, EventContext context)
    {
        _receivedEventsAndContexts.Add(new(@event, context));
        _readModels.Add(initial);
        return null; // Always return null to indicate deletion
    }
}