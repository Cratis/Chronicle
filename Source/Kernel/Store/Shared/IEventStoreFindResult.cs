// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Aksio.Cratis.Events.Store;

/// <summary>
/// Defines the result of a find within the event store.
/// </summary>
public interface IEventStoreFindResult
{
    /// <summary>
    /// Gets an observable <see cref="Subject{T}"/> that will receive all the <see cref="AppendedEvent"/> from a find.
    /// </summary>
    Subject<AppendedEvent> Event { get; }

    /// <summary>
    /// Create an enumerable of all the <see cref="AppendedEvent"/> in the find result.
    /// </summary>
    /// <returns>A collection of <see cref="AppendedEvent"/>.</returns>
    IEnumerable<AppendedEvent> AsEnumerable();
}
