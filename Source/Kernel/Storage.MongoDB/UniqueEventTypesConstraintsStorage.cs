// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IUniqueConstraintsStorage"/>.
/// </summary>
public class UniqueEventTypesConstraintsStorage : IUniqueEventTypesConstraintsStorage
{
    /// <inheritdoc/>
    public Task<bool> Exists(EventType eventType, EventSourceId eventSourceId, out EventSequenceNumber sequenceNumber) => throw new NotImplementedException();
}
