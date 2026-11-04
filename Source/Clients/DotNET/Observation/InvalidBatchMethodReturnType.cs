// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when a reactor batch method returns something other than <see cref="Task"/> or void.
/// </summary>
/// <param name="observerType">The observer <see cref="Type"/>.</param>
/// <param name="method">The name of the method.</param>
/// <param name="returnType">The return <see cref="Type"/>.</param>
public class InvalidBatchMethodReturnType(Type observerType, string method, Type returnType)
    : Exception($"Batch method '{method}' on '{observerType.FullName}' returns '{returnType.FullName}'. A reactor batch method returns Task or void; returning events from a batch method is not supported.");
