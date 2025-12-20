// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

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
    /// Find a read model by key, or return an empty object if not found.
    /// </summary>
    /// <param name="key">Key of the read model to find.</param>
    /// <returns>A read model instance with the data from the source, or an empty object.</returns>
    Task<ExpandoObject?> FindOrDefault(Key key);

    /// <summary>
    /// Find the root key of a read model that contains a child with the specified value at the given property path.
    /// </summary>
    /// <param name="childPropertyPath">The property path to the child property (e.g., "configurations.configurationId").</param>
    /// <param name="childValue">The value to search for.</param>
    /// <returns>An <see cref="Option{T}"/> containing the root key if found, otherwise None.</returns>
    /// <remarks>
    /// This method is used to resolve parent keys when projecting nested children.
    /// For optimal performance, ensure the child property path has an index defined using <c>[Index]</c> attribute.
    /// </remarks>
    Task<Option<Key>> TryFindRootKeyByChildValue(PropertyPath childPropertyPath, object childValue);

    /// <summary>
    /// Update or insert read model based on key.
    /// </summary>
    /// <param name="key">Key of the read model to upsert.</param>
    /// <param name="changeset">All changes in the form of a <see cref="Changeset{Event, ExpandoObject}"/>.</param>
    /// <param name="eventSequenceNumber">The sequence number of the event that caused the changes.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber);

    /// <summary>
    /// Enter replay state.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task BeginReplay(ReplayContext context);

    /// <summary>
    /// Re-enter replay state.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task ResumeReplay(ReplayContext context);

    /// <summary>
    /// End replay state.
    /// </summary>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task EndReplay(ReplayContext context);

    /// <summary>
    /// Prepare the sink for an initial run.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Typically the sink will clear out any existing data. This is to be able to guarantee
    /// the idempotency of the projection.
    /// </remarks>
    Task PrepareInitialRun();

    /// <summary>
    /// Ensure that all required indexes are created on the sink.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method is called during projection registration to ensure that all indexes
    /// defined on the read model are created. This is important for performance when
    /// using nested children with <c>UsingParentKeyFromContext</c>.
    /// </remarks>
    Task EnsureIndexes();
}
