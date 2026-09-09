// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// The exception that is thrown when a storage provider does not support event sequence mutation registries.
/// </summary>
/// <param name="operation">The unsupported registry operation.</param>
public class EventSequenceMutationRegistryNotSupported(string operation) : Exception($"The event sequence mutation registry operation '{operation}' is not supported by this storage provider.")
{
    /// <summary>
    /// Gets the unsupported operation.
    /// </summary>
    public string Operation { get; } = operation;
}
