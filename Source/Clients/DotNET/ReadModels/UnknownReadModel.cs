// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Exception that gets thrown when a read model type is not known by projections or reducers.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="UnknownReadModel"/>.
/// </remarks>
/// <param name="readModelType">The read model type that is unknown.</param>
public class UnknownReadModel(Type readModelType) : Exception($"Read model type '{readModelType.Name}' is not known by any projections or reducers")
{
    /// <summary>
    /// Gets the read model type that is unknown.
    /// </summary>
    public Type ReadModelType { get; } = readModelType;
}
