// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Integration.for_ReadModelReactors;

[DependencyInjection.IgnoreConvention]
public class RecordingReducerReadModelReactor : IReadModelReactor
{
    readonly Subject<ReadModelChangeset<ReducerWatchedReadModel>> _changes = new();
    readonly object _sync = new();

    public IObservable<ReadModelChangeset<ReducerWatchedReadModel>> Changes => _changes;

    public void Added(ReducerWatchedReadModel model, EventContext context) => Publish(model, context, ReadModelChangeType.Added);

    public void Modified(ReducerWatchedReadModel model, EventContext context) => Publish(model, context, ReadModelChangeType.Modified);

    public void Removed(ReducerWatchedReadModel? model, EventContext context) => Publish(model, context, ReadModelChangeType.Removed);

    public void Close() => _changes.Dispose();

    void Publish(ReducerWatchedReadModel? model, EventContext context, ReadModelChangeType type)
    {
        lock (_sync)
        {
            _changes.OnNext(new("default", context.EventSourceId.Value, model, type == ReadModelChangeType.Removed, type, context));
        }
    }
}
