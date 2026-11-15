// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a batch method declares a parameter Chronicle cannot supply when it calls the method.
/// </summary>
/// <param name="observerType">The observer <see cref="Type"/>.</param>
/// <param name="method">The name of the method.</param>
/// <param name="reason">What is wrong with the parameter.</param>
public class BatchMethodParameterCannotBeSupplied(Type observerType, string method, string reason)
    : Exception($"Batch method '{method}' on '{observerType.FullName}' cannot be called: {reason}. After its leading parameters a batch method takes a BatchContext, a CancellationToken or services.");
