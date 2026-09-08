// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// The exception that is thrown when a projection operation returns a result the caller cannot interpret.
/// </summary>
/// <param name="operation">The operation that returned the result.</param>
public class UnexpectedProjectionResult(string operation)
    : Exception($"Unexpected result type from {operation}.");
