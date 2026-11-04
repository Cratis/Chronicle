// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a method that does not take an <see cref="EventBatch"/> declares <see cref="ObservesAttribute"/>.
/// </summary>
/// <param name="observerType">The observer <see cref="Type"/>.</param>
/// <param name="method">The name of the method.</param>
public class ObservesOnlyOnBatchMethods(Type observerType, string method)
    : Exception($"Method '{method}' on '{observerType.FullName}' declares [Observes<T>], which only applies to a method that takes an EventBatch as its first parameter.");
