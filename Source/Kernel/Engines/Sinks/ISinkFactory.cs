// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Models;
using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Engines.Sinks;

/// <summary>
/// Defines a factory that can create instances of <see cref="ISink"/> for a specific <see cref="SinkTypeId"/>.
/// </summary>
public interface ISinkFactory
{
    /// <summary>
    /// Gets the <see cref="SinkTypeId"/> that identifies the type of store the factory supports.
    /// </summary>
    SinkTypeId TypeId { get; }

    /// <summary>
    /// Create a <see cref="ISink"/> for a specific <see cref="Model"/>.
    /// </summary>
    /// <param name="model"><see cref="Model"/> to create for.</param>
    /// <returns>A new instance of <see cref="ISink"/> for the <see cref="Model"/>.</returns>
    ISink CreateFor(Model model);
}
