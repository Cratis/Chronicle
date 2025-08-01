// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Changes;

/// <summary>
/// Defines the storage mechanism for changesets. Typically used for debugging purposes to see what changes has occurred.
/// </summary>
public interface IChangesetStorage
{
    /// <summary>
    /// Begin replay of a specific <see cref="ReadModelName"/>.
    /// </summary>
    /// <param name="model">The <see cref="ReadModelName"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task BeginReplay(ReadModelName model);

    /// <summary>
    /// Begin replay of a specific <see cref="ReadModelName"/>.
    /// </summary>
    /// <param name="model">The <see cref="ReadModelName"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task EndReplay(ReadModelName model);

    /// <summary>
    /// Save changesets associated with a specific <see cref="CorrelationId"/>.
    /// </summary>
    /// <param name="model">The <see cref="ReadModelName"/>.</param>
    /// <param name="modelKey">The <see cref="Key"/>.</param>
    /// <param name="eventType">The <see cref="EventType"/> that was at the root.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/>.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> to save for.</param>
    /// <param name="changeset">All the associated <see cref="IChangeset{Event, ExpandoObject}">changesets</see>.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(
        ReadModelName model,
        Key modelKey,
        EventType eventType,
        EventSequenceNumber sequenceNumber,
        CorrelationId correlationId,
        IChangeset<AppendedEvent, ExpandoObject> changeset);
}
