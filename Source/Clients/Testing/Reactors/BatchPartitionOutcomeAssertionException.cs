// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// The exception that is thrown when an assertion about the partitions a reactor batch method failed or deferred fails.
/// </summary>
/// <param name="message">The message describing the failed assertion.</param>
public class BatchPartitionOutcomeAssertionException(string message) : Exception(message);
