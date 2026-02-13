// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines a type that has a read model.
/// </summary>
public interface IHaveReadModel
{
    /// <summary>
    /// Gets the type of the read model.
    /// </summary>
    Type ReadModelType { get; }

    /// <summary>
    /// Gets the container name of the read model (collection, table, etc.).
    /// </summary>
    ReadModelContainerName ContainerName { get; }
}
