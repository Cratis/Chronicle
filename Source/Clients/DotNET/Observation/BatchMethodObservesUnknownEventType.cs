// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a batch method observes a type that is not a known event type.
/// </summary>
/// <param name="observerType">The observer <see cref="Type"/>.</param>
/// <param name="method">The name of the method.</param>
/// <param name="eventType">The observed <see cref="Type"/>.</param>
public class BatchMethodObservesUnknownEventType(Type observerType, string method, Type eventType)
    : Exception($"Method '{method}' on '{observerType.FullName}' observes '{eventType.FullName}', which is not a known event type.");
