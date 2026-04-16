// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The exception that is thrown when a read model instance has not been registered for a specific key in test scenarios.
/// </summary>
/// <param name="readModelType">The read model type that was not found.</param>
/// <param name="key">The <see cref="ReadModelKey"/> for which no read model was registered.</param>
public class ReadModelNotRegistered(Type readModelType, ReadModelKey key)
    : Exception($"No read model of type '{readModelType.Name}' has been registered for key '{key.Value}'. Use ReadModel() on the Given builder to register an instance.")
{
    /// <summary>
    /// Gets the read model type that was not found.
    /// </summary>
    public Type ReadModelType { get; } = readModelType;

    /// <summary>
    /// Gets the key for which no read model was registered.
    /// </summary>
    public ReadModelKey Key { get; } = key;
}
