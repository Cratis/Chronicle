// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Defines a system that observes changes to read model collections through MongoDB change streams, sharing one
/// change stream between every observer of the same collection.
/// </summary>
public interface IReadModelChangeStreams
{
    /// <summary>
    /// Observe a collection, reading it once the change stream is open and again after every change to it.
    /// </summary>
    /// <typeparam name="T">Type of what a read produces.</typeparam>
    /// <param name="database">The <see cref="IMongoDatabase"/> holding the collection.</param>
    /// <param name="containerName">The name of the collection to observe.</param>
    /// <param name="read">The read to perform, given a <see cref="CancellationToken"/> that is cancelled when the subscription ends.</param>
    /// <returns>An observable emitting the result of each read, in order.</returns>
    /// <remarks>
    /// Reads for one subscription never overlap, and changes that arrive while a read is running are answered by
    /// a single read after it. A read or a change stream that fails transiently is retried with a short backoff;
    /// the observable only fails once the failure persists.
    /// </remarks>
    IObservable<T> Observe<T>(IMongoDatabase database, string containerName, Func<CancellationToken, Task<T>> read);
}
