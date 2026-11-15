// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a <see cref="BatchSizeAttribute"/> declares a negative bound.
/// </summary>
/// <param name="observerType">The observer <see cref="Type"/>.</param>
/// <param name="property">The name of the property with the negative value.</param>
/// <param name="value">The negative value.</param>
public class InvalidBatchSize(Type observerType, string property, int value)
    : Exception($"[BatchSize] on '{observerType.FullName}' sets {property} to {value}. A bound is zero, to keep its default, or positive.");
