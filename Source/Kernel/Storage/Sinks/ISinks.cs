// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines a system for working with available <see cref="ISink">projection sinks</see>.
/// </summary>
public interface ISinks
{
    /// <summary>
    /// Check if there is a <see cref="ISink"/> of a specific <see cref="SinkTypeId"/> and <see cref="SinkConfigurationId"/>  registered in the system.
    /// </summary>
    /// <param name="typeId"><see cref="SinkTypeId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    bool HasType(SinkTypeId typeId);

    /// <summary>
    /// Get a <see cref="ISink"/> of a specific <see cref="SinkTypeId"/>.
    /// </summary>
    /// <param name="typeId"><see cref="SinkTypeId"/> to get for.</param>
    /// /// <param name="configurationId"><see cref="SinkConfigurationId"/> to get for.</param>
    /// <param name="model"><see cref="Model"/> to get for.</param>
    /// <returns><see cref="ISink"/> instance.</returns>
    ISink GetFor(SinkTypeId typeId, SinkConfigurationId configurationId, Model model);
}
