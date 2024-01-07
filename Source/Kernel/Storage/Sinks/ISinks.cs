// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;
using Aksio.Cratis.Sinks;

namespace Aksio.Cratis.Kernel.Storage.Sinks;

/// <summary>
/// Defines a system for working with available <see cref="ISink">projection sinks</see>.
/// </summary>
public interface ISinks
{
    /// <summary>
    /// Check if there is a <see cref="ISink"/> of a specific <see cref="SinkTypeId"/> registered in the system.
    /// </summary>
    /// <param name="typeId"><see cref="SinkTypeId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    bool HasType(SinkTypeId typeId);

    /// <summary>
    /// Get a <see cref="ISink"/> of a specific <see cref="SinkTypeId"/>.
    /// </summary>
    /// <param name="typeId"><see cref="SinkTypeId"/> to get for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> the sink is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the sink is for.</param>
    /// <param name="model"><see cref="Model"/> to get for.</param>
    /// <returns><see cref="ISink"/> instance.</returns>
    ISink GetFor(SinkTypeId typeId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Model model);
}
