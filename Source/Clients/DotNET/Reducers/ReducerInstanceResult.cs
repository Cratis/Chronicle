// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Represents the result of a reducer GetInstanceById operation.
/// </summary>
/// <param name="ReadModel">The instance of the read model.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record ReducerInstanceResult(object? ReadModel, EventSequenceNumber LastHandledEventSequenceNumber);

/// <summary>
/// Represents the result of a reducer GetInstanceById operation.
/// </summary>
/// <typeparam name="T">Type of read model.</typeparam>
/// <param name="ReadModel">The instance of the read model.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record ReducerInstanceResult<T>(T? ReadModel, EventSequenceNumber LastHandledEventSequenceNumber);
