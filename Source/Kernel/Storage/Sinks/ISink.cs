// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Changes;
using Cratis.Chronicle.Keys;
using Cratis.Events;
using Cratis.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines the storage for changes after events has been observed.
/// </summary>
public interface ISink
{
    /// <summary>
    /// Gets the <see cref="SinkTypeId"/> that identifies the store.
    /// </summary>
    SinkTypeId TypeId { get; }

    /// <summary>
    /// Gets the display friendly <see cref="SinkTypeName"/>.
    /// </summary>
    SinkTypeName Name { get; }

    /// <summary>
    /// Find a model by key, or return an empty object if not found.
    /// </summary>
    /// <param name="key">Key of the model to find.</param>
    /// <returns>A model instance with the data from the source, or an empty object.</returns>
    Task<ExpandoObject?> FindOrDefault(Key key);

    /// <summary>
    /// Update or insert model based on key.
    /// </summary>
    /// <param name="key">Key of the model to upsert.</param>
    /// <param name="changeset">All changes in the form of a <see cref="Changeset{Event, ExpandoObject}"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset);

    /// <summary>
    /// Enter replay state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task BeginReplay();

    /// <summary>
    /// End replay state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task EndReplay();

    /// <summary>
    /// Prepare the sink for an initial run.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Typically the sink will clear out any existing data. This is to be able to guarantee
    /// the idempotency of the projection.
    /// </remarks>
    Task PrepareInitialRun();
}
