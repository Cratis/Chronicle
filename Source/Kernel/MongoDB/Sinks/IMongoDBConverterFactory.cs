// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Defines a factory for <see cref="IMongoDBConverter"/>.
/// </summary>
public interface IMongoDBConverterFactory
{
    /// <summary>
    /// Create a <see cref="IMongoDBConverter"/> for a given <see cref="Model"/>.
    /// </summary>
    /// <param name="model">The <see cref="Model"/> to create for.</param>
    /// <returns>A <see cref="IMongoDBConverter"/> instance.</returns>
    IMongoDBConverter CreateFor(Model model);
}
