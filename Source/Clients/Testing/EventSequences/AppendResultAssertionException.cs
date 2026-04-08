// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// The exception that is thrown when an assertion on an <see cref="IAppendResult"/> fails.
/// </summary>
/// <param name="message">The message describing why the assertion failed.</param>
public class AppendResultAssertionException(string message) : Exception(message);
