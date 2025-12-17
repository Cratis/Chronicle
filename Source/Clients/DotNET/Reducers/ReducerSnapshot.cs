// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents a snapshot of a reducer at a specific point in time grouped by CorrelationId.
/// </summary>
/// <typeparam name="TReadModel">Type of read model.</typeparam>
/// <param name="Instance">The reduced instance of the read model.</param>
/// <param name="Events">The deserialized events that were applied.</param>
/// <param name="Occurred">When the first event occurred.</param>
/// <param name="CorrelationId">The CorrelationId the events were for.</param>
public record ReducerSnapshot<TReadModel>(TReadModel Instance, IEnumerable<AppendedEvent> Events, DateTimeOffset Occurred, CorrelationId CorrelationId);
