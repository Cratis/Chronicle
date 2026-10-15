// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents a pending save for a resolved future that must be applied after the main changeset save.
/// </summary>
/// <param name="Key">The key to use when applying the changeset.</param>
/// <param name="Changeset">The changeset containing the future's resolved changes.</param>
public record PendingFutureSave(Key Key, IChangeset<AppendedEvent, ExpandoObject> Changeset)
{
    /// <summary>
    /// Gets the persisted future to remove only after this write succeeds.
    /// </summary>
    public ProjectionFutureId? FutureId { get; init; }
}
