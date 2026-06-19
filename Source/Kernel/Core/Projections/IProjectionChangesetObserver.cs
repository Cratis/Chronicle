// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines an internal grain observer that receives projection changeset notifications.
/// </summary>
public interface IProjectionChangesetObserver : IGrainObserver
{
    /// <summary>
    /// Called when a projection produces a new changeset for a read model instance.
    /// </summary>
    /// <param name="namespaceName">The <see cref="EventStoreNamespaceName"/> the changeset belongs to.</param>
    /// <param name="readModelKey">The <see cref="ReadModelKey"/> identifying the read model instance.</param>
    /// <param name="readModel">The serialized read model as a <see cref="JsonObject"/>.</param>
    /// <param name="change">The <see cref="ReadModelChangeContext"/> describing the change and the event that caused it.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task OnChangeset(EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change);
}
