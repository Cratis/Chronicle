// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a method that takes an <see cref="EventBatch"/> declares no <see cref="ObservesAttribute"/>.
/// </summary>
/// <param name="observerType">The observer <see cref="Type"/>.</param>
/// <param name="method">The name of the method.</param>
public class BatchMethodObservesNothing(Type observerType, string method)
    : Exception($"Method '{method}' on '{observerType.FullName}' takes an EventBatch but declares no event types. Add [Observes<TEvent>] once for each event type it handles.");
