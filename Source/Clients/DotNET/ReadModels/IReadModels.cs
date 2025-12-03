// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a system that works with read models in the system.
/// </summary>
public interface IReadModels
{
    /// <summary>
    /// Register the read models in the system.
    /// </summary>
    /// <returns>An awaitable task.</returns>
    Task Register();

    /// <summary>
    /// Register a specific read model type.
    /// </summary>
    /// <typeparam name="TReadModel">The type of the read model to register.</typeparam>
    /// <returns>An awaitable task.</returns>
    Task Register<TReadModel>();
}
