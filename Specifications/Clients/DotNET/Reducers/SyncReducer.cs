// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Objects;

namespace Cratis.Chronicle.Reducers;

public class SyncReducer
{
    List<EventAndContext> _receivedEventsAndContexts = [];
    List<ReadModel> _readModels = [];

    public IEnumerable<EventAndContext> ReceivedEventsAndContexts => _receivedEventsAndContexts;
    public IEnumerable<ValidEvent> ReceivedEvents => _receivedEventsAndContexts.Select(_ => _.Event).Cast<ValidEvent>().ToArray();
    public IEnumerable<EventContext> ReceivedEventContexts => _receivedEventsAndContexts.Select(_ => _.Context).ToArray();
    public IEnumerable<ReadModel> ReceivedReadModels => _readModels;
    public ReadModel ReturnedReadModel { get; private set; }

    public ReadModel Reduce(ValidEvent @event, ReadModel? initial, EventContext context)
    {
        _receivedEventsAndContexts.Add(new(@event, context));
        _readModels.Add(initial?.Clone() ?? null!);
        ReturnedReadModel = new(initial?.Count + 1 ?? 1);
        return ReturnedReadModel;
    }
}
