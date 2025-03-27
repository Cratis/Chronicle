// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.Changes;

/// <summary>
/// Represents a changeset for a model.
/// </summary>
/// <param name="Id">The unique identifier - composite key.</param>
/// <param name="EventType">Type of event that caused the change.</param>
/// <param name="Changes">The actual changes.</param>
public record ModelChangeset(ModelChangeKey Id, EventType EventType, IEnumerable<Change> Changes);
