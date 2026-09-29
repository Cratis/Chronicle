// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

/// <summary>
/// An <see cref="IReadModelChangeStreams"/> that reads only when told to, so a spec decides exactly when a read
/// happens instead of racing a real change stream.
/// </summary>
public class a_manually_read_change_streams : IReadModelChangeStreams
{
    Func<Task>? _read;

    /// <inheritdoc/>
    public IObservable<T> Observe<T>(IMongoDatabase database, string containerName, Func<CancellationToken, Task<T>> read) =>
        Observable.Create<T>(observer =>
        {
            _read = async () => observer.OnNext(await read(CancellationToken.None));
            return Disposable.Empty;
        });

    /// <summary>
    /// Perform one read of the observed collection and deliver its result, if any, to the subscriber.
    /// </summary>
    /// <returns>A <see cref="Task"/> completing when the read has been delivered.</returns>
    public Task Read() => _read!();
}
