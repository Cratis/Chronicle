// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[DependencyInjection.IgnoreConvention]
public class RecordingReadModelReactor : IReadModelReactor
{
    public TaskCompletionSource AddedSignal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public WatchedReadModel? AddedModel { get; private set; }

    public EventContext? AddedContext { get; private set; }

    public Task Added(WatchedReadModel model, EventContext context)
    {
        AddedModel = model;
        AddedContext = context;
        AddedSignal.TrySetResult();
        return Task.CompletedTask;
    }
}
