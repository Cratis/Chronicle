// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Projections;

/// <summary>
/// The exception that is thrown when a projection operation returns a result the caller does not know how to map.
/// </summary>
/// <param name="operation">The operation that produced the result.</param>
/// <param name="resultType">The type of the result that was produced.</param>
public class UnexpectedProjectionResult(string operation, Type resultType)
    : Exception($"'{operation}' produced a result of type '{resultType.FullName}', which it has no mapping for");
