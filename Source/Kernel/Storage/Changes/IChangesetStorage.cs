// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Changes;

/// <summary>
/// Defines the storage mechanism for changesets. Typically used for debugging purposes to see what changes has occurred.
/// </summary>
public interface IChangesetStorage
{
    /// <summary>
    /// Save changesets associated with a specific <see cref="CorrelationId"/>.
    /// </summary>
    /// <param name="correlationId"><see cref="CorrelationId"/> to save for.</param>
    /// <param name="associatedChangeset">All the associated <see cref="IChangeset{Event, ExpandoObject}">changesets</see>.</param>
    /// <returns>Async task.</returns>
    Task Save(CorrelationId correlationId, IChangeset<AppendedEvent, ExpandoObject> associatedChangeset);
}
