// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Cuts;

/// <summary>
/// The exception that is thrown when a storage provider does not support read-model cut storage.
/// </summary>
/// <param name="operation">The unsupported operation.</param>
public class ReadModelCutStorageNotSupported(string operation) : Exception($"The read model cut storage operation '{operation}' is not supported by this storage provider.")
{
    /// <summary>
    /// Gets the unsupported operation.
    /// </summary>
    public string Operation { get; } = operation;
}
