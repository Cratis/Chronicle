// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Changes;

/// <summary>
/// Represents a null implementation of <see cref="IChangesetStorage"/>.
/// </summary>
public class NullChangesetStorage : IChangesetStorage
{
    /// <inheritdoc/>
    public Task BeginReplay(ReadModelContainerName readModel) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task EndReplay(ReadModelContainerName readModel) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Save(ReadModelContainerName readModel, Key readModelKey, EventType eventType, EventSequenceNumber sequenceNumber, CorrelationId correlationId, IChangeset<AppendedEvent, ExpandoObject> changeset) => Task.CompletedTask;
}
