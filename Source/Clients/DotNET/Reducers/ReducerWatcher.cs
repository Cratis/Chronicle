// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerWatcher{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">Type of read model the watcher is for.</typeparam>
public class ReducerWatcher<TReadModel> : IReducerWatcher<TReadModel>, IDisposable
{
    readonly Subject<ReducerChangeset<TReadModel>> _subject = new();

    /// <inheritdoc/>
    public IObservable<ReducerChangeset<TReadModel>> Observable => _subject;

    /// <inheritdoc/>
    public void Start()
    {
    }

    /// <inheritdoc/>
    public void Stop()
    {
        _subject.OnCompleted();
    }

    /// <inheritdoc/>
    public void Dispose() => _subject.Dispose();

    /// <summary>
    /// Notify the watcher of a change.
    /// </summary>
    /// <param name="namespace">The namespace for the event store.</param>
    /// <param name="modelKey">The <see cref="ReadModelKey"/> for the model.</param>
    /// <param name="readModel">The instance of the read model.</param>
    /// <param name="removed">Whether the read model was removed.</param>
    internal void NotifyChange(EventStoreNamespaceName @namespace, ReadModelKey modelKey, TReadModel? readModel, bool removed)
    {
        _subject.OnNext(new ReducerChangeset<TReadModel>(@namespace, modelKey, readModel, removed));
    }
}
