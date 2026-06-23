// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReactorInvoker;

public class RecordingReactor : IReadModelReactor
{
    public WatchedReadModel? AddedModel { get; private set; }

    public EventContext? AddedContext { get; private set; }

    public IEnumerable<WatchedReadModel> ModifiedModels { get; private set; } = [];

    public WatchedReadModel? RemovedModel { get; private set; }

    public OutboundEvent? ReturnedEvent { get; private set; }

    public Task Added(WatchedReadModel model, EventContext context)
    {
        AddedModel = model;
        AddedContext = context;
        return Task.CompletedTask;
    }

    public void Modified(IEnumerable<WatchedReadModel> models) => ModifiedModels = models;

    public OutboundEvent Removed(WatchedReadModel model)
    {
        RemovedModel = model;
        ReturnedEvent = new OutboundEvent();
        return ReturnedEvent;
    }
}
