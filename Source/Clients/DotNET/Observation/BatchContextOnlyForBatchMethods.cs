// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a per-event method takes a <see cref="BatchContext"/>.
/// </summary>
/// <param name="observerType">The observer <see cref="Type"/>.</param>
/// <param name="method">The name of the method.</param>
public class BatchContextOnlyForBatchMethods(Type observerType, string method)
    : Exception($"Method '{method}' on '{observerType.FullName}' takes a BatchContext, which is only available to a method that takes an EventBatch.");
