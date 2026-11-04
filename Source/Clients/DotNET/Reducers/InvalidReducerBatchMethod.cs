// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// The exception that is thrown when a reducer batch method does not have the expected signature.
/// </summary>
/// <param name="reducerType">The reducer <see cref="Type"/>.</param>
/// <param name="method">The name of the method.</param>
/// <param name="reason">What to change.</param>
public class InvalidReducerBatchMethod(Type reducerType, string method, string reason)
    : Exception($"Batch method '{method}' on reducer '{reducerType.FullName}' has an unsupported signature. {reason}");
