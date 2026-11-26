// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Exception raised when a read-model assertion does not match the materialized state.
/// </summary>
/// <param name="message">The expected and actual state difference.</param>
public class ReadModelAssertionFailed(string message) : Exception(message);
