// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Represents the result of an projection.
/// </summary>
/// <param name="ReadModel">The instance of the read model.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record ProjectionResult(object ReadModel, int ProjectedEventsCount, EventSequenceNumber LastHandledEventSequenceNumber);

/// <summary>
/// Represents the result of an projection.
/// </summary>
/// <typeparam name="T">Type of read model.</typeparam>
/// <param name="ReadModel">The instance of the read model.</param>
/// <param name="ProjectedEventsCount">Number of events that caused projection.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record ProjectionResult<T>(T ReadModel, int ProjectedEventsCount, EventSequenceNumber LastHandledEventSequenceNumber);
